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

void IrReceived(int data)
{
	switch(data){
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
	case IR_CODE_INVALID:
		break;
	default:
		if(-256 < data && data < 256) MotorPwm(data);
		break;
	}
	//fprintf(&USBSerialStream, "received : %d\r\n", sdata);
}

int main(void)
{
	SetupHardware();
	
	CDC_Device_CreateStream(&VirtualSerial_CDC_Interface, &USBSerialStream);

	GlobalInterruptEnable();

	int  cnt;
	char str[256];
	for(cnt = 0; ; cnt++){
		char c = CDC_Device_ReceiveByte(&VirtualSerial_CDC_Interface);
		if(isalpha(c)){
			fputc(c   , &USBSerialStream);
			fputc('\n', &USBSerialStream);
		}

		sprintf(str, "%d \r\n", cnt);
		
		//fputs(str, &USBSerialStream);
			
#if IR_RECEIVER
		{
			int data = IrReceive();
			if(data != IR_CODE_INVALID) IrReceived(data);
		}
#if 0
		RELAY_OFF;
		if(cnt % 4 == 0) RELAY_ON;
		LEDR_OFF;
		LEDG_OFF;
		if(cnt % 2 == 0) LEDR_ON;
		if(cnt % 2 == 1) LEDG_ON;
#endif
#else
#if TEST_BOARD
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
#endif

		CDC_Device_USBTask(&VirtualSerial_CDC_Interface);
		USB_USBTask();
		
		_delay_ms(300);
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

	sbi(DDRC, 7);	// BOARD LED

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
#endif
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
