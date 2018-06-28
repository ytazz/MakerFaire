/**
    Maker Faire 2018
    モータコントローラ
 **/

/** pin config
	 1 GND
	 2 5V
	 3 ~RESET
	 4 PD0 OC0B INT0    -> CH1 A
	 5 PD1      INT1    -> CH2 A
	 6 PD2      INT2    -> CH3 A
	 7 PD3      INT3
	 8 PD4      ADC8    -> CH1 B
	 9 PD5              -> CH2 B
	10 PD6 OC4D  ADC9   -> CH3 B
	11 PD7 OC4D~ ADC10
	12 PC6 OC3A OC4A~
	13 PC7      OC4A
	14 PF0 ADC0
	15 PF1 ADC1
	16 PE2 *書き込み用

	17 GND
	18 PF4 ADC4
	19 PF5 ADC5
	20 PF6 ADC6
	21 PF7 ADC7
	22 PB0                  -> CH1 DIR
	23 PB1                  -> CH2 DIR
	24 PB2                  -> CH3 DIR
	25 PB3
	26 PB4 ADC11
	27 PB5 ADC12 OC1A OC4B~ -> CH1 PWM
	28 PB6 ADC13 OC1B OC4B  -> CH2 PWM
	29 PB7       OC0A OC1C  -> CH3 PWM
	30 PE6 INT6
	31 VREF
	32 GND
	
	- ADCの参照電圧をAVCCにとるように設定
	- USBを使うのははまりそうなのでUART＋FTDIケーブルを使用

 */

#include <avr/io.h>
#include <avr/wdt.h>
#include <avr/power.h>
#include <avr/interrupt.h>
#include <util/delay.h>
#include <string.h>
#include <ctype.h>
#include <stdio.h>
#include <math.h>

#include "Descriptors.h"

#include <LUFA/Drivers/USB/USB.h>
#include <LUFA/Platform/Platform.h>

void SetupHardware(void);
void CheckJoystickMovement(void);

void EVENT_USB_Device_Connect(void);
void EVENT_USB_Device_Disconnect(void);
void EVENT_USB_Device_ConfigurationChanged(void);
void EVENT_USB_Device_ControlRequest(void);

USB_ClassInfo_CDC_Device_t VirtualSerial_CDC_Interface = {
	.Config = {
		.ControlInterfaceNumber = INTERFACE_ID_CDC_CCI,
		.DataINEndpoint = {
			.Address = CDC_TX_EPADDR,
			.Size    = CDC_TXRX_EPSIZE,
			.Banks   = 1,
		},
		.DataOUTEndpoint = {
			.Address = CDC_RX_EPADDR,
			.Size    = CDC_TXRX_EPSIZE,
			.Banks   = 1,
		},
		.NotificationEndpoint = {
			.Address = CDC_NOTIFICATION_EPADDR,
			.Size    = CDC_NOTIFICATION_EPSIZE,
			.Banks   = 1,
		},
	},
};

static FILE USBSerialStream;

uint32_t cnt_ms;          //< ms after program start
uint32_t toggle_time[3];  //< last rise time of encoder signal
bool     wait_rise[3];
bool     enabled;         //< drive motor or not
int      mode    [3];     //< 0: command pwm_ref  1: command pos_ref
int16_t  pos     [3];     //< encoder count
int16_t  pos_ref [3];     //< encoder count reference signal
bool     dir     [3];
uint8_t  pwm     [3];     //< pwm duty rate. 0, 255
int16_t  pwm_ref [3];
bool     polarity[3];     //< motor polarity

const uint8_t gain[] = {0, 150, 255};   //< pwm value v.s. position error
const int     nlevel = 2;              //< number of gain levels

int main(void){
	SetupHardware();
	
	CDC_Device_CreateStream(&VirtualSerial_CDC_Interface, &USBSerialStream);

	GlobalInterruptEnable();

	char strRecv[256];
	char strSend[256];
	char cmd[256];
	for(;;){
		// receive command string
	    uint16_t num = CDC_Device_BytesReceived(&VirtualSerial_CDC_Interface);
	    if(num > 0){
	    	for(int i = 0; i < num; i++)
	    		strRecv[i] = CDC_Device_ReceiveByte(&VirtualSerial_CDC_Interface);
	    	strRecv[num] = '\0';
	    	
	    	// echo
	    	//fputs(strRecv, &USBSerialStream);
	    	
	    	// start
	    	// stop
	    	// set ref0 ref1 ref2
	    	sscanf(strRecv, "%s", cmd);
	    	if(strcmp(cmd, "enable") == 0){
	    		enabled = true;
	    	}
	    	if(strcmp(cmd, "disable") == 0){
	    		enabled = false;
	    	}
	    	if(strcmp(cmd, "set") == 0){
	    		sscanf(strRecv, "%s %d %d %d %d %d %d %d %d %d",
	    			cmd,
	    			&mode[0], &mode[1], &mode[2],
	    		 	&pos_ref[0], &pos_ref[1], &pos_ref[2],
	    		 	&pwm_ref[0], &pwm_ref[1], &pwm_ref[2]
	    		 	);
	    	}
	    }
	    
		if(cnt_ms % 50 == 0){
			sprintf(strSend, "%d %d %d %d %d %d %d %d %d\r\n", pos[0], pos[1], pos[2], pwm[0], pwm[1], pwm[2], dir[0], dir[1], dir[2]);
			fputs(strSend, &USBSerialStream);
		}
		/*
		// sinusoidal reference (for testing)
		if(cnt_ms % 10 == 0){
			pos_ref[0] = (int)(10.0f*sinf((float)cnt_ms/1000.0f));
			pos_ref[1] = (int)(10.0f*sinf((float)cnt_ms/1000.0f));
			pos_ref[2] = (int)(10.0f*sinf((float)cnt_ms/1000.0f));
		}
		*/
		
		// calculate motor command
		for(int i = 0; i < 3; i++){
			if(mode[i] == 0){
				if(pwm_ref[i] > 0){
					pwm[i] = pwm_ref[i];
					dir[i] = false;
				}
				else{
					pwm[i] = (uint8_t)(-pwm_ref[i]);
					dir[i] = true;
				}
			}
			if(mode[i] == 1){
				int16_t e = pos_ref[i] - pos[i];
				int16_t eabs;
				if(e > 0){
				    dir[i] = !polarity[i];
				    eabs   = e;
				}
				else{
				 	dir[i] =  polarity[i];
				 	eabs   = -e;
				}
				pwm[i] = (eabs >= nlevel ? gain[nlevel-1] : gain[eabs]);
			}
		}
		
		OCR1AL = pwm[0];
		OCR1BL = pwm[1];
		OCR1CL = pwm[2];
		
		if(dir[0])
			 PORTB |=  _BV(0);
		else PORTB &= ~_BV(0);
		if(dir[1])
			 PORTB |=  _BV(1);
		else PORTB &= ~_BV(1);
		if(dir[2])
			 PORTB |=  _BV(2);
		else PORTB &= ~_BV(2);

		CDC_Device_USBTask(&VirtualSerial_CDC_Interface);
		USB_USBTask();
		
		_delay_us(100);
	}
}

void SetupHardware(void){
	MCUSR &= ~(1 << WDRF);
	wdt_disable();

	clock_prescale_set(clock_div_1);

	USB_Init();
	
	//
	DDRB = 0xe7;  // 11100111
	
	// configure port D as inputs
	DDRD = 0x00;  // 00000000
	
	// pull-up enable for encoder pins
	PORTD = 0xff; // 11111111
	
	EICRA = 0x15;		//< 00010101 interrupt on rising and falling edge of INT0|1|2
	EIMSK = 0x07;       //<	00000111 enable external interrupt on INT0|1|2
	
	// timer0 for timing generation
	TCCR0A = 0x00;  //< normal operation
	TCCR0B = 0x03;  //< i/o clock 16Mhz  1/64 prescaling; overflow makes 1ms period
	TIMSK0 = 0x01;  //< overflow interrupt enable
	                
	// timer1 for pwm
	TCCR1A = 0xa9;  //< 10101001 fast pwm 8bit, output from OC1A|B|C
	TCCR1B = 0x0a;  //< 00001010 fast pwm 8bit, i/o clock with 1/8 prescaling = 2MHz  pwm=8kHz
	TIMSK1 = 0x01;  //< overflow interrupt enable (just for checking pwm frequency)
	
	cnt_ms = 0;
	
	for(int i = 0; i < 3; i++){
		toggle_time[i] = 0;
    	pos        [i] = 0;
		dir        [i] = false;
		pwm        [i] = 0;
		polarity   [i] = false;
	}

	// wait until i/o port stabilizes
	_delay_us(100);
	
	// if input pin is high, wait for falling edge, other wise wait for rising edge
	wait_rise[0] = !(PIND & _BV(0));
	
	// initially not enabled
	enabled = false;

}

// external input interrupt handlers
ISR(INT0_vect){
	if(cnt_ms - toggle_time[0] > 1){
		// on rising edge
		if(wait_rise[0] && (PIND & _BV(0)) ){
			if(PIND & _BV(4))
				 pos[0]++;
			else pos[0]--;
			wait_rise[0] = false;
			toggle_time[0] = cnt_ms;
		}
		// on falling edge
		if(!wait_rise[0] && !(PIND & _BV(0)) ){
			if(PIND & _BV(4))
				 pos[0]--;
			else pos[0]++;
			wait_rise[0] = true;
			toggle_time[0] = cnt_ms;
		}
	}
}

ISR(INT1_vect){
	if(cnt_ms - toggle_time[1] > 1){
		// on rising edge
		if(wait_rise[1] && (PIND & _BV(1)) ){
			if(PIND & _BV(5))
				 pos[1]++;
			else pos[1]--;
			wait_rise[1] = false;
			toggle_time[1] = cnt_ms;
		}
		// on falling edge
		if(!wait_rise[1] && !(PIND & _BV(1)) ){
			if(PIND & _BV(5))
				 pos[1]--;
			else pos[1]++;
			wait_rise[1] = true;
			toggle_time[1] = cnt_ms;
		}
	}
}

ISR(INT2_vect){
	if(cnt_ms - toggle_time[2] > 1){
		// on rising edge
		if(wait_rise[2] && (PIND & _BV(2)) ){
			if(PIND & _BV(6))
				 pos[2]++;
			else pos[2]--;
			wait_rise[2] = false;
			toggle_time[2] = cnt_ms;
		}
		// on falling edge
		if(!wait_rise[2] && !(PIND & _BV(2)) ){
			if(PIND & _BV(6))
				 pos[2]--;
			else pos[2]++;
			wait_rise[2] = true;
			toggle_time[2] = cnt_ms;
		}
	}
}

// timer0 overflow interrupt handler
ISR(TIMER0_OVF_vect){
	cnt_ms++;
	TCNT0 = 5;
}

// timer1 overflow interrupt handler
ISR(TIMER1_OVF_vect){
	TCNT1L = 5;
}

void EVENT_USB_Device_Connect               (void){}
void EVENT_USB_Device_Disconnect            (void){}
void EVENT_USB_Device_ConfigurationChanged  (void){ CDC_Device_ConfigureEndpoints(&VirtualSerial_CDC_Interface); }
void EVENT_USB_Device_ControlRequest        (void){ CDC_Device_ProcessControlRequest(&VirtualSerial_CDC_Interface); }
void EVENT_CDC_Device_ControLineStateChanged(USB_ClassInfo_CDC_Device_t *const CDCInterfaceInfo){}

