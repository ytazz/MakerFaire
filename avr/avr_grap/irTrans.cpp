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
        *data = results.value >> 1;
        recv.resume();
    }
    return ret ? 1 : 0;
}

// ------------------------------------------------------------

extern "C" void IrSend(int data)
{
    IrRawSend(data, IR_SEND_BITS);
}

extern "C" int IrReceive()
{
    unsigned long data;
    if(IrRawRecive(&data)){
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
