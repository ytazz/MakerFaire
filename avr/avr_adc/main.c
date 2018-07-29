/**
    Maker Faire 2018
    モータコントローラ
 **/

/** pin config
	 1 GND
	 2 5V
	 3 ~RESET
	 4 PD0 OC0B INT0
	 5 PD1      INT1
	 6 PD2      INT2
	 7 PD3      INT3
	 8 PD4      ADC8
	 9 PD5          
	10 PD6 OC4D  ADC9
	11 PD7 OC4D~ ADC10
	12 PC6 OC3A OC4A~
	13 PC7      OC4A
	14 PF0 ADC0
	15 PF1 ADC1
	16 PE2 *書き込み用

	17 GND
	18 PF4 ADC4             -> POT1
	19 PF5 ADC5             -> POT2
	20 PF6 ADC6             -> POT3
	21 PF7 ADC7             -> POT4
	22 PB0                  -> SW1
	23 PB1                  -> SW2
	24 PB2                  -> SW3
	25 PB3                  -> SW4
	26 PB4 ADC11
	27 PB5 ADC12 OC1A OC4B~
	28 PB6 ADC13 OC1B OC4B
	29 PB7       OC0A OC1C
	30 PE6 INT6
	31 VREF
	32 GND

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

uint32_t cnt_ms;          //< ms after program start
uint16_t pot[4];
uint8_t  sw[4];

int main(void){
	SetupHardware();
	
	CDC_Device_CreateStream(&VirtualSerial_CDC_Interface, &USBSerialStream);

	GlobalInterruptEnable();

	char strSend[256];
	
	for(;;){
		if(cnt_ms % 50 == 0){
			pot[0] = GetAD(4);
			pot[1] = GetAD(5);
			pot[2] = GetAD(6);
			pot[3] = GetAD(7);
			sw [0] = !!(PINB & _BV(0));
			sw [1] = !!(PINB & _BV(1));
			sw [2] = !!(PINB & _BV(2));
			sw [3] = !!(PINB & _BV(3));
			
		
			sprintf(strSend, "%d %d %d %d %d %d %d %d\r\n",
			    pot[0], pot[1], pot[2], pot[3],
			    sw[0], sw[1], sw[2], sw[3]);
			fputs(strSend, &USBSerialStream);
		}

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
	
	// port B input
	DDRB = 0x00;
	
	// port F input
	DDRF = 0x00;
	
	// port B pull-up
	PORTB = 0x0f;  // 00001111
	
	// port F pull-up
	PORTF = 0xf0;  // 11110000
	
	// timer0 for timing generation
	TCCR0A = 0x00;  //< normal operation
	TCCR0B = 0x03;  //< i/o clock 16Mhz  1/64 prescaling; overflow makes 1ms period
	TIMSK0 = 0x01;  //< overflow interrupt enable
	
	// adc
	ADCSRA |= _BV(ADPS0);  // prescaler 128 -> ADC clock 125kHz
	ADCSRA |= _BV(ADPS1);
	ADCSRA |= _BV(ADPS2);
	ADCSRA |= _BV(ADEN); // A/D enable
	
	for(int i = 0; i < 4; i++){
		pot[i] = 0;
		sw [i] = 0;
	}
	cnt_ms = 0;
}

// timer0 overflow interrupt handler
ISR(TIMER0_OVF_vect){
	cnt_ms++;
	TCNT0 = 5;
}

void EVENT_USB_Device_Connect               (void){}
void EVENT_USB_Device_Disconnect            (void){}
void EVENT_USB_Device_ConfigurationChanged  (void){ CDC_Device_ConfigureEndpoints(&VirtualSerial_CDC_Interface); }
void EVENT_USB_Device_ControlRequest        (void){ CDC_Device_ProcessControlRequest(&VirtualSerial_CDC_Interface); }
void EVENT_CDC_Device_ControLineStateChanged(USB_ClassInfo_CDC_Device_t *const CDCInterfaceInfo){}

