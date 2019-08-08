#include "IRremote.h"
#include "IRremoteInt.h"
#include "GrapCommon.h"

void TIMER_CONFIG_NORMAL()
{
	TCCR4A = 0;
	TCCR4B = _BV(CS40);	/* Prescaler 1/1 */
	TCCR4C = 0;
	TCCR4D = 0;
	TCCR4E = 0;
	TC4H   = (SYSCLOCK * USECPERTICK / 1000000) >> 8;
	OCR4C  = (SYSCLOCK * USECPERTICK / 1000000) & 255;
	TC4H   = 0;
	TCNT4  = 0;
}


#ifdef IR_TIMER_USE_ESP32
hw_timer_t *timer;
void IRTimer(); // defined in IRremote.cpp
#endif

//+=============================================================================
// Decodes the received IR message
// Returns 0 if no data ready, 1 if data ready.
// Results of decoding are stored in results
//
bool  IRrecv::decode (decode_results *results)
{
	results->rawbuf   = irparams.rawbuf;
	results->rawlen   = irparams.rawlen;

	results->overflow = irparams.overflow;

	if (irparams.rcvstate != STATE_STOP)  return false ;

	if(!decodeMain(results)){
		// Throw away and start over
		resume();
		return false;
	}else{
		return true;
	}
}

bool  IRrecv::decodeMain (decode_results *results)
{
	unsigned long  data   = 0;
	int   offset = 0;  // Dont skip first space, check its size

	if (irparams.rawlen < (2 * IR_SEND_BITS) + 2)  return false ;

	offset++;

	// Initial mark
	if (!MATCH_MARK(results->rawbuf[offset++], IR_HDR_MARK))  return false ;

	while (offset + 1 < irparams.rawlen) {
		if (!MATCH_SPACE(results->rawbuf[offset++], IR_HDR_SPACE))  break ;

		if      (MATCH_MARK(results->rawbuf[offset], IR_ONE_MARK))   data = (data << 1) | 1 ;
		else if (MATCH_MARK(results->rawbuf[offset], IR_ZERO_MARK))  data = (data << 1) | 0 ;
		else                                                           return false ;
		offset++;
	}

	// Success
	results->bits = (offset - 1) / 2;
	results->value = data;
	return true;
}

//+=============================================================================
IRrecv::IRrecv ()
{
}

//+=============================================================================
// initialization
//
void  IRrecv::enableIRIn ( )
{
// Interrupt Service Routine - Fires every 50uS
#ifdef ESP32
	// ESP32 has a proper API to setup timers, no weird chip macros needed
	// simply call the readable API versions :)
	// 3 timers, choose #1, 80 divider nanosecond precision, 1 to count up
	timer = timerBegin(1, 80, 1);
	timerAttachInterrupt(timer, &IRTimer, 1);
	// every 50ns, autoreload = true
	timerAlarmWrite(timer, 50, true);
	timerAlarmEnable(timer);
#else
	cli();
	// Setup pulse clock timer interrupt
	// Prescale /8 (16M/8 = 0.5 microseconds per tick)
	// Therefore, the timer interval can range from 0.5 to 128 microseconds
	// Depending on the reset value (255 to 0)
	TIMER_CONFIG_NORMAL();

	// Timer2 Overflow Interrupt Enable
	TIMER_ENABLE_INTR;

	TIMER_RESET;

	sei();  // enable interrupts
#endif

#if 0
	// Set pin modes
	pinMode(irparams.recvpin, INPUT);
#endif
	// Initialize state machine variables
	irparams.rcvstate = STATE_IDLE;
	irparams.rawlen = 0;
}

//+=============================================================================
// Return if receiving new IR signals
//
bool  IRrecv::isIdle ( )
{
	return (irparams.rcvstate == STATE_IDLE || irparams.rcvstate == STATE_STOP) ? true : false;
}
//+=============================================================================
// Restart the ISR state machine
//
void  IRrecv::resume ( )
{
	irparams.rcvstate = STATE_IDLE;
	irparams.rawlen = 0;
}

//+=============================================================================
// hashdecode - decode an arbitrary IR code.
// Instead of decoding using a standard encoding scheme
// (e.g. Sony, NEC, RC5), the code is hashed to a 32-bit value.
//
// The algorithm: look at the sequence of MARK signals, and see if each one
// is shorter (0), the same length (1), or longer (2) than the previous.
// Do the same with the SPACE signals.  Hash the resulting sequence of 0's,
// 1's, and 2's to a 32-bit value.  This will give a unique value for each
// different code (probably), for most code systems.
//
// http://arcfn.com/2010/01/using-arbitrary-remotes-with-arduino.html
//
// Compare two tick values, returning 0 if newval is shorter,
// 1 if newval is equal, and 2 if newval is longer
// Use a tolerance of 20%
//
int  IRrecv::compare (unsigned int oldval,  unsigned int newval)
{
	if      (newval < oldval * .8)  return 0 ;
	else if (oldval < newval * .8)  return 2 ;
	else                            return 1 ;
}
