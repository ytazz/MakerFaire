#include "IRremote.h"
#include "GrapCommon.h"

static IRrecv recv;
static IRsend send;

extern "C" void IrSendSetup()
{
    send.enableIROut(38);

    sbi(DDRC, 6);	// IR OUT
#if TEST_BOARD
    sbi(PINC, 6);	// IR OUT logical not
#endif
}

extern "C" void IrRawSend(unsigned long data, int nbits)
{
    send.send(data, nbits);
}

extern "C" void IrReceiveSetup()
{
    cbi(DDRE, 6);	// IR IN
    recv.enableIRIn();
}

extern "C" int IrRawRecive(unsigned long *data)
{
    decode_results results;
    bool ret = recv.decode(&results);
    if(ret){
        *data = results.value >> 1; // somehow required >> 1
        recv.resume();
    }
    return ret ? 1 : 0;
}

// ------------------------------------------------------------

unsigned char CalcCheckSum(unsigned long data)
{
    unsigned char mask = ((unsigned char)1 << IR_PARITY_BITS) - 1;
    unsigned char sum = 0;
    unsigned char i;
    for(; data != 0; data >>= IR_PARITY_BITS) sum += data & mask;
    return sum & mask;
}

extern "C" void IrSend(int data)
{
    data &= (_BV(IR_SEND_BITS) - 1);
    unsigned long sum = CalcCheckSum(data);
    data += (sum << IR_SEND_BITS);
    IrRawSend(data, IR_SEND_BITS + IR_PARITY_BITS);
}

extern "C" int IrReceive()
{
    unsigned long data;
    if(IrRawRecive(&data)){
        unsigned long p = (data >> IR_SEND_BITS) & (_BV(IR_PARITY_BITS) - 1);
        data &= (_BV(IR_SEND_BITS) - 1);
        unsigned long sum = CalcCheckSum(data);
        if(p != sum) return IR_CODE_INVALID;
#if 0
        char str[32];
        int i;
        for(i = 0; i < 16; i++)
            str[i] = (data & _BV(i)) ? '1' : '0';
        str[16] = '\0';
        fprintf(&USBSerialStream, "received : %s\r\n", str);	
#endif
        if(data & _BV(IR_SEND_BITS-1)){
            data = ~data & ((1UL << IR_SEND_BITS) - 1);
            data += 1;
            return -(int)data;
        }else{
            return (int)data;
        }        
    }else{
        return IR_CODE_INVALID;
    }
}
