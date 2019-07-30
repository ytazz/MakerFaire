//******************************************************************************
// IRremote
// Version 2.0.1 June, 2015
// Copyright 2009 Ken Shirriff
// For details, see http://arcfn.com/2009/08/multi-protocol-infrared-remote-library.html
//
// Modified by Paul Stoffregen <paul@pjrc.com> to support other boards and timers
//
// Interrupt code based on NECIRrcv by Joe Knapp
// http://www.arduino.cc/cgi-bin/yabb2/YaBB.pl?num=1210243556
// Also influenced by http://zovirl.com/2008/11/12/building-a-universal-remote-with-an-arduino/
//
// JVC and Panasonic protocol added by Kristian Lauszus (Thanks to zenwheel and other people at the original blog post)
// Whynter A/C ARC-110WD added by Francesco Meschia
//******************************************************************************

#ifndef IRremoteint_h
#define IRremoteint_h

#if 0
//------------------------------------------------------------------------------
// Include the right Arduino header
//
#if defined(ARDUINO) && (ARDUINO >= 100)
#	include <Arduino.h>
#else
#	if !defined(IRPRONTO)
#		include <WProgram.h>
#	endif
#endif
#endif

#include <avr/io.h>
#include <avr/sfr_defs.h>
#include <avr/interrupt.h>
#include <util/delay.h>



//+=============================================================================
// Sends an IR mark for the specified number of microseconds.
// The mark output is modulated at the PWM frequency.
//
#define mark(time) TIMER_ENABLE_PWM,_delay_us(time)

//+=============================================================================
// Leave pin off for time (given in microseconds)
// Sends an IR space for the specified number of microseconds.
// A space is no output, so the PWM output is disabled.
//
#define space(time) TIMER_DISABLE_PWM,_delay_us(time)

//------------------------------------------------------------------------------
// This handles definition and access to global variables
//
#ifdef IR_GLOBAL
#	define EXTERN
#else
#	define EXTERN extern
#endif

//------------------------------------------------------------------------------
// Information for the Interrupt Service Routine
//
#define RAWBUF  101  // Maximum length of raw duration buffer

typedef
	struct {
		// The fields are ordered to reduce memory over caused by struct-padding
		uint8_t       rcvstate;        // State Machine state
#if 0
		uint8_t       recvpin;         // Pin connected to IR data from detector
		uint8_t       blinkpin;
		uint8_t       blinkflag;       // true -> enable blinking of pin on IR processing
#endif
		uint8_t       rawlen;          // counter of entries in rawbuf
		unsigned int  timer;           // State timer, counts 50uS ticks.
		unsigned int  rawbuf[RAWBUF];  // raw data
		uint8_t       overflow;        // Raw buffer overflow occurred
	}
irparams_t;

// ISR State-Machine : Receiver States
#define STATE_IDLE      2
#define STATE_MARK      3
#define STATE_SPACE     4
#define STATE_STOP      5
#define STATE_OVERFLOW  6

// Allow all parts of the code access to the ISR data
// NB. The data can be changed by the ISR at any time, even mid-function
// Therefore we declare it as "volatile" to stop the compiler/CPU caching it
EXTERN  volatile irparams_t  irparams;

//------------------------------------------------------------------------------
// Defines for setting and clearing register bits
//
#ifndef cbi
#	define cbi(sfr, bit)  (_SFR_BYTE(sfr) &= ~_BV(bit))
#endif

#ifndef sbi
#	define sbi(sfr, bit)  (_SFR_BYTE(sfr) |= _BV(bit))
#endif

//------------------------------------------------------------------------------
// Pulse parms are ((X*50)-100) for the Mark and ((X*50)+100) for the Space.
// First MARK is the one after the long gap
// Pulse parameters in uSec
//

// Due to sensor lag, when received, Marks  tend to be 100us too long and
//                                   Spaces tend to be 100us too short
#define MARK_EXCESS    100

// Upper and Lower percentage tolerances in measurements
#define TOLERANCE       25
#define LTOL            (1.0 - (TOLERANCE/100.))
#define UTOL            (1.0 + (TOLERANCE/100.))

// Minimum gap between IR transmissions
#define _GAP            5000
#define GAP_TICKS       (_GAP/USECPERTICK)

#define TICKS_LOW(us)   ((int)(((us)*LTOL/USECPERTICK)))
#define TICKS_HIGH(us)  ((int)(((us)*UTOL/USECPERTICK + 1)))

//------------------------------------------------------------------------------
// IR detector output is active low
//
#define MARK   0
#define SPACE  1

// All board specific stuff has been moved to its own file, included here.
#ifndef __AVR_ATmega32U4__
#define __AVR_ATmega32U4__
#endif
//#include "boarddefs.h"

//------------------------------------------------------------------------------
// CPU Frequency
//
#ifdef F_CPU
#	define SYSCLOCK  F_CPU     // main Arduino clock
#else
#	define SYSCLOCK  16000000  // main Arduino clock
#endif

// microseconds per clock interrupt tick
#define USECPERTICK    50

//---------------------------------------------------------
// Timer4 (10 bits, high speed option)
//

#define TIMER_RESET

#define TIMER_ENABLE_INTR   (TIMSK4 = _BV(TOIE4))
#define TIMER_DISABLE_INTR  (TIMSK4 = 0)

//#if defined(ARDUINO_AVR_PROMICRO) // Sparkfun Pro Micro 
#if 1                        
	#define TIMER_ENABLE_PWM    (TCCR4A |= _BV(COM4A0))     // Use complimentary O̅C̅4̅A̅ output on pin 5
	#define TIMER_DISABLE_PWM   (TCCR4A &= ~(_BV(COM4A0)))  // (Pro Micro does not map PC7 (32/ICP3/CLK0/OC4A)
															// of ATmega32U4 )
#else
	#define TIMER_ENABLE_PWM    (TCCR4A |= _BV(COM4A1))
	#define TIMER_DISABLE_PWM   (TCCR4A &= ~(_BV(COM4A1)))
#endif

#endif
