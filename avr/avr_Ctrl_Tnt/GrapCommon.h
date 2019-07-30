#ifndef GRAP_COMMON
#define GRAP_COMMON

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

#define IR_RECEIVER 0
#define TEST_BOARD 0

// The transmitter side ------------------------------

#define LEDM_ON  sbi(PORTC, 7)
#define LEDM_OFF cbi(PORTC, 7)
#define LEDR_ON  sbi(PORTF, 4)
#define LEDR_OFF cbi(PORTF, 4)
#define LEDG_ON  sbi(PORTF, 5)
#define LEDG_OFF cbi(PORTF, 5)
#define LEDB_ON  sbi(PORTF, 6)
#define LEDB_OFF cbi(PORTF, 6)
#define LEDW_ON  sbi(PORTF, 7)
#define LEDW_OFF cbi(PORTF, 7)
#define SW_RELAY        (PINB & _BV(4))
#define SW_MOTOR_ON     (PINB & _BV(3))
#define SW_MOTOR_ON_INV (PINB & _BV(6))

// The receiver side ------------------------------

#define LED7SEG_RCK_1 sbi(PORTB, 0)
#define LED7SEG_RCK_0 cbi(PORTB, 0)
#define RELAY_ON  sbi(PORTB, 1)
#define RELAY_OFF cbi(PORTB, 1)
#define MOTOR_ENABLE  sbi(PORTB, 4)
#define MOTOR_DISABLE cbi(PORTB, 4)
#define MOTOR_INV_1 sbi(PORTB, 2)
#define MOTOR_INV_0 cbi(PORTB, 2)

// IR Transmissions ------------------------------

#define IR_SEND_BITS              10
#define IR_PARITY_BITS             2

#define IR_HDR_MARK             2400
#define IR_HDR_SPACE             600
#define IR_ONE_MARK             1200
#define IR_ZERO_MARK             600
#define IR_RPT_LENGTH          45000
#define IR_DOUBLE_SPACE_USECS    500  // usually ssee 713 - not using ticks as get number wrapround

#define IR_CODE_INVALID         (1<<IR_SEND_BITS)
#define IR_CODE_RELAY_ON         361
#define IR_CODE_RELAY_OFF        362
#define IR_CODE_MOTOR_OFF        363
#endif