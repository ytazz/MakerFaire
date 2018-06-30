#include <avr/io.h>
#include <avr/sfr_defs.h>
#include <avr/interrupt.h>
#include <util/delay.h>

#include "GrapCommon.h"

#define NUM_DIGITS 4
static const uint8_t g_LedDigitPatterns[] = {0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x27, 0x7F, 0x6F};
static const uint8_t g_MinusMarkPattern = 0x40;
static const uint8_t g_EmptyPattern = 0;
static const uint8_t g_DotPattern = 0x80;

volatile uint8_t g_DisplayPatterns[NUM_DIGITS];
volatile uint8_t g_CurrentDigit = 0;

void Led7Seg_Init()
{
    /* Init SPI */
    SPCR = 0b01010000;
    SPSR = 0b00000001;

	sbi(DDRB, 0);	// RCK
    sbi(DDRB, 1);   // SCLK
	sbi(DDRB, 2);   // MOSI

#if 1
    /* Interval interrupt by using timer0 */
    TCCR0A = 0; // Normal port operation, OC0A disconnected.
	TCCR0B = _BV(CS02); // Prescaler 1/256 => 62.5KHz
	TCNT0  = 0;
	OCR0A  = 63;
    //TIFR0  = _BV(OCF0A);
    //TIMSK0 = _BV(OCIE0A);
    TIMSK0 = _BV(TOIE0);
#endif
#if 0
#ifdef F_CPU
#	define SYSCLOCK  F_CPU     // main Arduino clock
#else
#	define SYSCLOCK  16000000  // main Arduino clock
#endif

// microseconds per clock interrupt tick
#define USECPERTICK    50

 	TCCR4A = 0;
	TCCR4B = _BV(CS40);	/* Prescaler 1/1 */
	TCCR4C = 0;
	TCCR4D = 0;
	TCCR4E = 0;
	TC4H   = (SYSCLOCK * USECPERTICK / 1000000) >> 8;
	OCR4C  = (SYSCLOCK * USECPERTICK / 1000000) & 255;
	TC4H   = 0;
	TCNT4  = 0;
    TIMSK4 = _BV(TOIE4);
#endif
}

void SendShiftReg(uint8_t data)
{
    SPDR = ~data;    // start transmission
    while((SPSR & _BV(SPIF)) == 0);    // wait until complete
}

void Led7Seg_SetDisplayNumber(int16_t val)
{
    int i;
    int absval = val;

    if(val < 0){
        absval = -val;
    }

    for(i = 0; i < NUM_DIGITS; i++){
        int n = absval % 10;
        g_DisplayPatterns[i] = g_LedDigitPatterns[n];
        absval /= 10;
        if(absval == 0){
            for(i++; i < NUM_DIGITS; i++) g_DisplayPatterns[i] = 0;
            break;
        }
    }

    if(val < 0)
        g_DisplayPatterns[NUM_DIGITS - 1] = g_MinusMarkPattern;
}

void Led7Seg_flip()
{
    SendShiftReg(1 << g_CurrentDigit);
    SendShiftReg(g_DisplayPatterns[g_CurrentDigit]);

    // Send RCK
    LED7SEG_RCK_0;
    LED7SEG_RCK_1;

    g_CurrentDigit++;
    if(g_CurrentDigit == NUM_DIGITS) g_CurrentDigit = 0;
}

//+=============================================================================
// Interrupt Service Routine - Fires every 1ms
//
ISR (TIMER0_OVF_vect)
{
    SendShiftReg(1 << g_CurrentDigit);
    SendShiftReg(g_DisplayPatterns[g_CurrentDigit]);

    // Send RCK
    LED7SEG_RCK_0;
    LED7SEG_RCK_1;

    g_CurrentDigit++;
    if(g_CurrentDigit == NUM_DIGITS) g_CurrentDigit = 0;
}

