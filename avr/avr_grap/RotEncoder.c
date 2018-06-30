#include <avr/io.h>
#include <avr/sfr_defs.h>
#include <avr/interrupt.h>
#include <util/delay.h>

#include "GrapCommon.h"
#include "RotEncoder.h"
#include "led7seg.h"
#include "irTrans.h"

volatile static char g_OldRot;
volatile static int g_Value = 0;

void InitRotEncoder()
{
    sbi(PCICR, PCIE0);
    cbi(PCIFR, PCIE0);
    //sbi(PCMSK0, PCINT2);
    sbi(EIMSK, INT2);
	cbi(DDRD, 2);	// ENCODER A in
	cbi(DDRD, 3);	// ENCODER B in
#if !TEST_BOARD
	sbi(PORTD, 2);	// ENCODER A pull-up
	sbi(PORTD, 3);	// ENCODER B pull-up
#endif
    Led7Seg_SetDisplayNumber(g_Value);
}

//+=============================================================================
// Interrupt Service Routine
//
ISR(INT2_vect)
{
    if(bit_is_set(PIND, PORTD2)){
        g_OldRot = bit_is_set(PIND, PORTD3) ? 'R' : 'L';
    }else{
        if(bit_is_set(PIND, PORTD3)){
            if(g_OldRot == 'L'){
                g_Value--;
                Led7Seg_SetDisplayNumber(g_Value);
                IrSend(g_Value);
            }
        }else{
            if(g_OldRot == 'R'){
                g_Value++;
                Led7Seg_SetDisplayNumber(g_Value);
                IrSend(g_Value);
            }
        }
        g_OldRot = 0;
    }
}
