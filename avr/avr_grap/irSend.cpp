#include "IRremote.h"
#include "IRremoteInt.h"
#include "GrapCommon.h"

void TIMER_CONFIG_KHZ(int val)
{
  const uint16_t pwmval = SYSCLOCK / 2000 / (val);	/* The period is halved since Phase and Frequency Correct PWM mode */
  TCCR4A                = (1<<PWM4A);	/* PWM mode based on comparator OCR4A */
  TCCR4B                = _BV(CS40);	/* Prescaler 1/1 */
  TCCR4C                = 0;			    /* OC4D is disabled (Normal GPIO) */
  TCCR4D                = (1<<WGM40);	/* Phase and Frequency Correct PWM */
  TCCR4E                = 0;
  TC4H                  = pwmval >> 8;	/* TOP (High byte temporary buffer) */
  OCR4C                 = pwmval;	      /* TOP */
  TC4H                  = (pwmval / 3) >> 8;	/* duty ratio 1/3 (High byte temporary buffer) */
  OCR4A                 = (pwmval / 3) & 255;	/* duty ratio 1/3 */
}

#if 0
//+=============================================================================
void  IRsend::sendRaw (const unsigned int buf[],  unsigned int len,  unsigned int hz)
{
	// Set IR carrier frequency
	enableIROut(hz);

	for (unsigned int i = 0;  i < len;  i++) {
		if (i & 1)  space(buf[i]) ;
		else        mark (buf[i]) ;
	}

	space(0);  // Always end with the LED off
}

//+=============================================================================
// Sends an IR mark for the specified number of microseconds.
// The mark output is modulated at the PWM frequency.
//
void  IRsend::mark (unsigned int time)
{
	TIMER_ENABLE_PWM; // Enable pin 3 PWM output
	if (time > 0) custom_delay_usec(time);
}

//+=============================================================================
// Leave pin off for time (given in microseconds)
// Sends an IR space for the specified number of microseconds.
// A space is no output, so the PWM output is disabled.
//
void  IRsend::space (unsigned int time)
{
	TIMER_DISABLE_PWM; // Disable pin 3 PWM output
	if (time > 0) IRsend::custom_delay_usec(time);
}
#endif




//+=============================================================================
// Enables IR output.  The khz value controls the modulation frequency in kilohertz.
// The IR output will be on pin 3 (OC2B).
// This routine is designed for 36-40KHz; if you use it for other values, it's up to you
// to make sure it gives reasonable results.  (Watch out for overflow / underflow / rounding.)
// TIMER2 is used in phase-correct PWM mode, with OCR2A controlling the frequency and OCR2B
// controlling the duty cycle.
// There is no prescaling, so the output frequency is 16MHz / (2 * OCR2A)
// To turn the output on and off, we leave the PWM running, but connect and disconnect the output pin.
// A few hours staring at the ATmega documentation and this will all make sense.
// See my Secrets of Arduino PWM at http://arcfn.com/2009/07/secrets-of-arduino-pwm.html for details.
//
void  IRsend::enableIROut (int khz)
{
// FIXME: implement ESP32 support, see IR_TIMER_USE_ESP32 in boarddefs.h
#ifndef ESP32
	// Disable the Timer2 Interrupt (which is used for receiving IR)
	TIMER_DISABLE_INTR; //Timer2 Overflow Interrupt

#if 0
	pinMode(TIMER_PWM_PIN, OUTPUT);
	digitalWrite(TIMER_PWM_PIN, LOW); // When not sending PWM, we want it low
#endif

	// COM2A = 00: disconnect OC2A
	// COM2B = 00: disconnect OC2B; to send signal set to 10: OC2B non-inverted
	// WGM2 = 101: phase-correct PWM with OCRA as top
	// CS2  = 000: no prescaling
	// The top value for the timer.  The modulation frequency will be SYSCLOCK / 2 / OCR2A.
	TIMER_CONFIG_KHZ(khz);
#endif
}

#if 0
//+=============================================================================
// Custom delay function that circumvents Arduino's delayMicroseconds limit

#define clockCyclesPerMicrosecond() ( F_CPU / 1000000L )

unsigned long micros() {
    unsigned long m;
    uint8_t oldSREG = SREG, t;
     
    cli();
    m = timer0_overflow_count;
    t = TCNT0;
    t = TCNT0L;
 
    if ((TIFR0 & _BV(TOV0)) && (t < 255))
        m++;
 
    SREG = oldSREG;
     
    return ((m << 8) + t) * (64 / clockCyclesPerMicrosecond());
}

void IRsend::custom_delay_usec(unsigned long uSecs) {
  if (uSecs > 4) {
    unsigned long start = micros();
    unsigned long endMicros = start + uSecs - 4;
    if (endMicros < start) { // Check if overflow
      while ( micros() > start ) {} // wait until overflow
    }
    while ( micros() < endMicros ) {} // normal wait
  } 
  //else {
  //  __asm__("nop\n\t"); // must have or compiler optimizes out
  //}
}
#endif

//+=============================================================================
void  IRsend::send (unsigned long data,  int nbits)
{
	// Set IR carrier frequency
	enableIROut(38);

	// Header
	mark(IR_HDR_MARK);
	space(IR_HDR_SPACE);

	// Data
	for (unsigned long  mask = 1UL << (nbits - 1);  mask;  mask >>= 1) {
		if (data & mask) {
			mark(IR_ONE_MARK);
			space(IR_HDR_SPACE);
		} else {
			mark(IR_ZERO_MARK);
			space(IR_HDR_SPACE);
    	}
  	}

	// We will have ended with LED off
	mark(IR_ZERO_MARK);
}
