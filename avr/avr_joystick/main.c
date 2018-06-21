/**
    Maker Faire 2017 ジョイスティックインタフェースプログラム
 **/

/** pin config
	 1 GND
	 2 5V
	 3 ~RESET
	 4 PD0       LEDSW1_LED   OUT
	 5 PD1       LEDSW1_SW    IN PU
	 6 PD2       LEDSW2_LED   OUT
	 7 PD3       LEDSW2_SW    IN PU
	 8 PD4       LEDSW3_LED   OUT
	 9 PD5       LEDSW3_SW    IN PU
	10 PD6 ADC9  JOY1a        IN
	11 PD7 ADC10 JOY1b        IN
	12 PC6
	13 PC7
	14 PF0
	15 PF1
	16 PE2 *書き込み用

	17 GND
	18 PF4
	19 PF5
	20 PF6
	21 PF7
	22 PB0       SW4          IN PU
	23 PB1       SW3          IN PU
	24 PB2       SW2          IN PU
	25 PB3       SW1          IN PU
	26 PB4 ADC11 JOY2b        IN
	27 PB5 ADC12 JOY2a        IN
	28 PB6       LEDSW4_LED   IN PU
	29 PB7       LEDSW4_SW    OUT
	30 PE6
	31 VREF
	32 GND
	
	- ADCの参照電圧をAVCCにとるように設定

 */

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

int main(void){
	SetupHardware();
	
	CDC_Device_CreateStream(&VirtualSerial_CDC_Interface, &USBSerialStream);

	GlobalInterruptEnable();

	int  cnt = 0;
	char str[256];
	for(;;){
		char c = CDC_Device_ReceiveByte(&VirtualSerial_CDC_Interface);
		if(isalpha(c)){
			fputc(c   , &USBSerialStream);
			fputc('\n', &USBSerialStream);
		}
		
		if(cnt % 500 == 0){
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
			sprintf(str, "%d %d %d %d %d %d %d %d %d %d %d %d\r\n",
			 joy1a, joy1b, joy2a, joy2b,
			 sw1, sw2, sw3, sw4,
			 ledsw1, ledsw2, ledsw3, ledsw4);
		
			fputs(str, &USBSerialStream);
			
			uint8_t led1 = ledsw1;
			uint8_t led2 = ledsw2;
			uint8_t led3 = ledsw3;
			uint8_t led4 = ledsw4;
			
			if(led1)
				 PORTD |=  _BV(0);
			else PORTD &= ~_BV(0);
			if(led2)
				 PORTD |=  _BV(2);
			else PORTD &= ~_BV(2);
			if(led3)
				 PORTD |=  _BV(4);
			else PORTD &= ~_BV(4);
			if(led4)
				 PORTB |=  _BV(7);
			else PORTB &= ~_BV(7);
			
			cnt = 0;
		}

		CDC_Device_USBTask(&VirtualSerial_CDC_Interface);
		USB_USBTask();
		
		_delay_us(100);
		cnt++;
	}
}

void SetupHardware(void){
	MCUSR &= ~(1 << WDRF);
	wdt_disable();

	clock_prescale_set(clock_div_1);

	USB_Init();
	
	DDRB = 0x80;  // 10000000
	DDRD = 0x15;  // 00010101
	
	// pull-up enable
	PORTB = 0x4f; // 01001111
	PORTD = 0x2a; // 00101010

	// ADC
	ADCSRA |= _BV(ADPS0);  // prescaler 128 -> ADC clock 125kHz
	ADCSRA |= _BV(ADPS1);
	ADCSRA |= _BV(ADPS2);
	ADCSRA |= _BV(ADEN); // A/D enable

}

void EVENT_USB_Device_Connect               (void){}
void EVENT_USB_Device_Disconnect            (void){}
void EVENT_USB_Device_ConfigurationChanged  (void){ CDC_Device_ConfigureEndpoints(&VirtualSerial_CDC_Interface); }
void EVENT_USB_Device_ControlRequest        (void){ CDC_Device_ProcessControlRequest(&VirtualSerial_CDC_Interface); }
void EVENT_CDC_Device_ControLineStateChanged(USB_ClassInfo_CDC_Device_t *const CDCInterfaceInfo){}

