/**
    Maker Faire 2018 - Grapple with a reaction wheel
 **/

#include <avr/io.h>
#include <avr/wdt.h>
#include <avr/power.h>
#include <avr/interrupt.h>
#include <util/delay.h>
#include <string.h>
#include <stdio.h>

#include "Descriptors.h"

#include <LUFA/Drivers/USB/USB.h>
#include <LUFA/Platform/Platform.h>

// ------------------------------------------------------------

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

FILE USBSerialStream;

const uint8_t ADMUXMask[] = {
	0x00, // ADC0  00000000
	0x01, // ADC1  00000001
	0,    // ADC2  NA
	0,    // ADC3  NA
	0x04, // ADC4  00000100
	0x05, // ADC5  00000101
	0x06, // ADC6  00000110
	0x07, // ADC7  00000111
	0x20, // ADC8  00100000
	0x21, // ADC9  00100001
	0x22, // ADC10 00100010
	0x23, // ADC11 00100011
	0x24, // ADC12 00100100
	0x25, // ADC13 00100101
};

void EVENT_USB_Device_Connect               (void){}
void EVENT_USB_Device_Disconnect            (void){}
void EVENT_USB_Device_ConfigurationChanged  (void){ CDC_Device_ConfigureEndpoints(&VirtualSerial_CDC_Interface); }
void EVENT_USB_Device_ControlRequest        (void){ CDC_Device_ProcessControlRequest(&VirtualSerial_CDC_Interface); }
void EVENT_CDC_Device_ControLineStateChanged(USB_ClassInfo_CDC_Device_t *const CDCInterfaceInfo){}

// ------------------------------------------------------------

void SetupHardware(void);
void CheckJoystickMovement(void);

#include "GrapCommon.h"
#include "led7seg.h"
#include "RotEncoder.h"
#include "irTrans.h"
#include "Motor.h"

// ------------------------------------------------------------

void SelectADChannel(int ch){
	int mask = 0x01;
	for(int i = 0; i < 5; i++){
		if(ADMUXMask[ch] & mask)
			 ADMUX |=  mask;
		else ADMUX &= ~mask;
		mask <<= 1;
	}
	if(ADMUXMask[ch] & mask)
		 ADCSRB |=  mask;
	else ADCSRB &= ~mask;
}

uint16_t GetAD(int ch){
	// select channel
	SelectADChannel(ch);
	
	// start conversion
	ADCSRA |= _BV(ADSC); 
	
	// wait till done
	while( (ADCSRA & _BV(ADSC)) );
	// clear the flag by writing one
	//ADCSRA |= _BV(ADIF);
	
	// get conversion result
	uint8_t lo = ADCL;
	uint8_t hi = ADCH;
	
	uint16_t val = (hi << 8) + lo;
	
	return val;
}

/*
#if 0
			uint16_t joy1a  = GetAD( 9);
			uint16_t joy1b  = GetAD(10);
			uint16_t joy2a  = GetAD(11);
			uint16_t joy2b  = GetAD(12);
			uint8_t  sw1    = !!(PINB & _BV(3));
			uint8_t  sw2    = !!(PINB & _BV(2));
			uint8_t  sw3    = !!(PINB & _BV(1));
			uint8_t  sw4    = !!(PINB & _BV(0));
			uint8_t  ledsw1 = !!(PIND & _BV(1));
			uint8_t  ledsw2 = !!(PIND & _BV(3));
			uint8_t  ledsw3 = !!(PIND & _BV(5));
			uint8_t  ledsw4 = !!(PINB & _BV(6));

		if(cnt & 1)
			PORTC |=  _BV(7);
		else
			PORTC &= ~_BV(7);
#endif
*/

void IrReceiveProc()
{
	int data = IrReceive();

	switch(data){
	case IR_CODE_INVALID:
		break;
	case IR_CODE_RELAY_ON:
		RELAY_ON;
		LEDG_ON;
		break;
	case IR_CODE_RELAY_OFF:
		RELAY_OFF;
		LEDG_OFF;
		break;
	case IR_CODE_MOTOR_OFF:
		MotorPwm(0);
		break;
	default:
		if(-256 < data && data < 256) MotorPwm(data);
		break;
	}
	//fprintf(&USBSerialStream, "received : %d\r\n", sdata);
}

void IrTransmissionProc(int cmd)
{
	{
		static int prev_data = 0;
		static int prev_relay = 0;
		int data = 0;
		int rdata = 0;
		static int ir_cmd_mode=0;
		int enc_data=0;
		
		uint16_t joy2a  = GetAD(7);		//右レバー左右（RWレバーmode用）
		uint8_t  sw_LR	= !(PINB & _BV(5));
		uint8_t  sw_RL	= !(PINB & _BV(7));
		
		
		enc_data = 3 * RotEncoderGetVal();	// 3 is due to usability
		if(enc_data <= -120) enc_data = -120;
		if(enc_data > 120) enc_data = 120;

		if(SW_MOTOR_ON == 0){
			data = rdata;
			LEDR_ON;
		}else{
			LEDR_OFF;
		}
		if(SW_MOTOR_ON_INV == 0){
			data =-rdata;
			LEDB_ON;
		}else{
			LEDB_OFF;
		}

		//IrSend(data);
		if(cmd==0){		//制御mode設定
			if(sw_LR == 0){			//RWレバーモード
				if(sw_RL == 0){		//グラップル低速
					ir_cmd_mode = 10;
					IrSend(ir_cmd_mode);
				}else{				//グラップル高速
					ir_cmd_mode = 30;
					IrSend(ir_cmd_mode);	
				}
			}else if(sw_LR == 1){	//RWジャイロモード
				if(sw_RL == 0){		//グラップル低速
					ir_cmd_mode = 20;
					IrSend(ir_cmd_mode);
				}else{				//グラップル高速
					ir_cmd_mode = 40;
					IrSend(ir_cmd_mode);
				}
			}
		}else if(cmd==1){			//RW出力
			if(ir_cmd_mode == 20 || ir_cmd_mode == 40){		//ジャイロモード
				IrSend(enc_data);
			}else{			//レバーモード
				IrSend(-joy2a/3);
			}
			
			
			//IrSend(-20);
		}else if(cmd==2){
			//IrSend(20);
		}else{
			IrSend(1);
		}
			
		prev_data = data;
		prev_relay = SW_RELAY;

	}
#if TEST_BOARD
	{
		//static int prev_relay = 0;
		int relay = (SW_RELAY == 0) ? 0 : 1;
		if(prev_relay != relay){
			IrSend((relay == 0) ? IR_CODE_RELAY_OFF : IR_CODE_RELAY_ON);
			(relay == 0) ? LEDG_OFF : LEDG_ON;
			prev_relay = relay;
		}
	}
#endif

}

void DebugLed(int cnt)
{
#if IR_RECEIVER
	RELAY_OFF;
	if(cnt % 4 == 0) RELAY_ON;
	LEDR_OFF;
	LEDG_OFF;
	if(cnt % 2 == 0) LEDR_ON;
	if(cnt % 2 == 1) LEDG_ON;
#else
	LEDM_OFF;
	LEDR_OFF;
	LEDG_OFF;
	LEDB_OFF;
	LEDW_OFF;
	if(cnt % 2 == 0) LEDM_ON;
	if(cnt % 4 == 0) LEDR_ON;
	if(cnt % 4 == 1) LEDG_ON;
	if(cnt % 4 == 2) LEDB_ON;
	if(cnt % 4 == 3) LEDW_ON;
#endif
}

int main(void)
{
	SetupHardware();
	
	CDC_Device_CreateStream(&VirtualSerial_CDC_Interface, &USBSerialStream);

	GlobalInterruptEnable();

	int  cnt;
	static int prev_relay = 0;
	static int prev_data  = 0;
	int data = 0;
	static int prev_motor_on = 0;
	static int prev_motor_on_inv = 0;
	char str[256];
	for(cnt = 0; ; cnt++){
		char c = CDC_Device_ReceiveByte(&VirtualSerial_CDC_Interface);
		if(isalpha(c)){
			fputc(c   , &USBSerialStream);
			fputc('\n', &USBSerialStream);
		}

		if(cnt % 100 == 0){			//20
			uint16_t joy1a  = GetAD(4);
			uint16_t joy1b  = GetAD(5);
			uint16_t joy2a  = GetAD(7);
			uint16_t joy2b  = GetAD(6);
			uint8_t  sw_LL	= !(PINB & _BV(4));
			uint8_t  sw_LR	= !(PINB & _BV(5));
			uint8_t  sw_joy1= !(PINB & _BV(3));
			uint8_t  sw_RL	= !(PINB & _BV(7));
			uint8_t  sw_RR	= !(PINE & _BV(6));
			uint8_t  sw_joy2= !(PINB & _BV(6));
			uint8_t  sw_EMG	= !!(PIND & _BV(0));
			uint8_t  sw_HOME= !(PIND & _BV(1));
			uint8_t  GRAP_rot;
			uint8_t  sw_C1	= !(PIND & _BV(4));
			uint8_t  sw_C2	= !(PIND & _BV(5));
			uint8_t  sw_C3	= !(PIND & _BV(6));

			int rdata = 3 * RotEncoderGetVal();	// 3 is due to usability
			if(rdata <= -256) rdata = -255;
			if(rdata > 256) rdata = 255;
			
			if(SW_MOTOR_ON == 0){
				data = rdata;
			}else{
				data = -rdata;
			}
			GRAP_rot = data;
			sprintf(str, "%d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d\r\n",
		     joy1a, joy1b, joy2a, joy2b,
		     sw_HOME, sw_LL, sw_LR, sw_C2,
		     sw_RL, sw_C3, sw_RR, sw_EMG,
		     sw_joy1, sw_C1, sw_joy2, GRAP_rot);

			fputs(str, &USBSerialStream);
			
			
			
			//赤外線出力
			/*if(cnt % 6000 == 0){
				IrTransmissionProc();
				cnt = 0;
			}*/
			if(cnt == 2000){
				IrTransmissionProc(0);
			}else if(cnt == 4000){
				IrTransmissionProc(1);
			}else if(cnt == 6000){
				IrTransmissionProc(2);
				cnt = 0;
			}
			
		//cnt = 0;
		}
			
#if IR_RECEIVER
		IrReceiveProc();
#else
		//IrTransmissionProc();
#endif

		CDC_Device_USBTask(&VirtualSerial_CDC_Interface);
		USB_USBTask();
		
#if IR_RECEIVER
		_delay_ms(100);
#else
		//_delay_us(1500);	
#endif
	}
}

void SetupHardware(void)
{
	MCUSR &= ~(1 << WDRF);
	wdt_disable();

	clock_prescale_set(clock_div_1);

	USB_Init();

	// disable JTAG on borad
	MCUCR = 0x80; MCUCR = 0x80;

#if IR_RECEIVER
	IrReceiveSetup();
	InitMotor();
#else
	IrSendSetup();
	Led7Seg_Init();
	InitRotEncoder();
#endif

	// ------------------------------------------------------------



#if IR_RECEIVER
	sbi(DDRB, 1);	// RELAY
	sbi(DDRF, 4);	// LED0
	sbi(DDRF, 5);	// LED1
	sbi(DDRF, 6);	// LED2
#else
#if TEST_BOARD
	sbi(DDRF, 4);	// LED0
	sbi(DDRF, 5);	// LED1
	sbi(DDRF, 6);	// LED2
	sbi(DDRF, 7);	// LED3
	cbi(DDRB, 4);	// SW RELAY
	cbi(DDRB, 5);	// SW MOTOR ON
	cbi(DDRB, 6);	// SW MOTOR ON INV
	sbi(PORTB, 5);	// pull-up enable
	sbi(PORTB, 6);	// pull-up enable
#endif
	cbi(DDRB,4);	//sw_LL
	cbi(DDRB,5);	//sw_LR
	cbi(DDRB,3);	//sw_joy1
	cbi(DDRB,7);	//sw_RL
	cbi(DDRE,6);	//sw_RR
	cbi(DDRB,6);	//sw_joy2
	cbi(DDRD,0);	//EMG
	cbi(DDRD,1);	//HOME
	cbi(DDRD,2);	//ENC-A
	cbi(DDRD,3);	//ENC-B
	cbi(DDRD,4);	//sw_c1
	cbi(DDRD,5);	//sw_c2
	cbi(DDRD,6);	//sw_c3

	sbi(DDRC, 7);	// BOARD LED
	sbi(DDRC, 6);	//IR-LED
	sbi(DDRB, 0);	//7seg
	sbi(DDRB, 1);	//7seg
	sbi(DDRB, 2);	//7seg


	// ADC
	ADMUX = 0x44;			; //0100 0100
	ADCSRA |= _BV(ADPS0);  // prescaler 128 -> ADC clock 125kHz
	ADCSRA |= _BV(ADPS1);
	ADCSRA |= _BV(ADPS2);
	ADCSRA |= _BV(ADEN); // A/D enable
	
	sbi(PORTB,4);	//pull-up enable (sw_LL)
	sbi(PORTB,5);	//pull-up enable (sw_LR)
	sbi(PORTB,3);	//pull-up enable (sw_joy1)
	sbi(PORTB,7);	//pull-up enable (sw_RL)
	sbi(PORTE,6);	//pull-up enable (sw_RR)

	sbi(PORTB,6);	//pull-up enable (sw_joy2)
	sbi(PORTD,0);	//pull-up enable (EMG)
	sbi(PORTD,1);	//pull-up enable (HOME)
	sbi(PORTD,4);	//pull-up enable (sw_c1)

	sbi(PORTD,5);	//pull-up enable (sw_c2)
	sbi(PORTD,6);	//pull-up enable (sw_c3)
	

	
#endif


#if 0
	// pull-up enable
	PORTB = 0x4f; // 01001111
	PORTD = 0x2a; // 00101010

	// ADC
	ADCSRA |= _BV(ADPS0);  // prescaler 128 -> ADC clock 125kHz
	ADCSRA |= _BV(ADPS1);
	ADCSRA |= _BV(ADPS2);
	ADCSRA |= _BV(ADEN); // A/D enable
#endif
}

// ------------------------------------------------------------
