// control of DC motor driver MC33926 for reaction wheel

#include <avr/io.h>
#include <avr/sfr_defs.h>
#include <avr/interrupt.h>
#include <util/delay.h>

#include "GrapCommon.h"

void InitMotor()
{
	sbi(DDRC,6);	//PWM（RW用）を出力に設定

	// Motor PWM (OC1A) ------------------------------------------------------------
	TCCR1A = _BV(WGM10) | _BV(WGM12) | _BV(COM1A1);	// Fast PWM, 8-bit, Clear on compare match, set at TOP(=0xFF)
	//TCCR1B = _BV(CS12) | _BV(CS10);	// Prescaler 1/1024
	TCCR1B = _BV(CS12);	// Prescaler 1/256
	//TCCR1B = _BV(CS11) | _BV(CS10);	// Prescaler 1/64
	//TCCR1B = _BV(CS11);	// Prescaler 1/8
	TCCR1C = 0;

	// Motor PWM (OC3A) ------------------------------------------------------------
	TCCR3A = _BV(WGM30) | _BV(WGM32) | _BV(COM3A1);	// Fast PWM, 8-bit, Clear on compare match, set at TOP(=0xFF)
	TCCR3B = _BV(CS32);	// Prescaler 1/256
	TCCR3C = 0;
	
	sbi(DDRB, 5);	// PWM(Grapple用）を出力に設定
//	sbi(DDRB, 4);	// ENABLE
//	sbi(DDRB, 2);	// INV

	MotorPwm(0);
	MotorPwm_RW(0);
}

void MotorPwm(int16_t x)
{
	if(x >= 0)
		MOTOR_INV_0;
	else
		MOTOR_INV_1;
	
	const uint8_t pwmval = (x >= 0) ? x : -x;
	OCR1A = pwmval;

	if(pwmval == 0){
		MOTOR_DISABLE;
	//	LEDR_OFF;
	//	LEDB_OFF;
	}else{
		MOTOR_ENABLE;
		if(x >= 0){
	//		LEDR_ON;
	//		LEDB_OFF;
		}else{
	//		LEDR_OFF;
	//		LEDB_ON;
		}
	}
}


void MotorPwm_RW(int16_t x)
{
	if(x >= 0)
		MOTOR_INV_0_RW;
	else
		MOTOR_INV_1_RW;
	
	const uint8_t pwmval = (x >= 0) ? x : -x;
	OCR3A = pwmval;

	if(pwmval == 0){
		MOTOR_DISABLE;
	//	LEDR_OFF;
	//	LEDB_OFF;
	}else{
		MOTOR_ENABLE;
		if(x >= 0){
	//		LEDR_ON;
	//		LEDB_OFF;
		}else{
	//		LEDR_OFF;
	//		LEDB_ON;
		}
	}
}

