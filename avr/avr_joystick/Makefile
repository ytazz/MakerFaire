#
# copied from LUFA VirtualSerial demo
#

MCU          = atmega32u4
ARCH         = AVR8
#BOARD        = USBKEY
F_CPU        = 16000000
F_USB        = $(F_CPU)
OPTIMIZATION = s
TARGET       = main
SRC          = $(TARGET).c Descriptors.c $(LUFA_SRC_USB) $(LUFA_SRC_USBCLASS)
LUFA_PATH    = C:\usr\Develop\avr\lufa-LUFA-170418\LUFA
CC_FLAGS     = -DUSE_LUFA_CONFIG_HEADER -IConfig/
LD_FLAGS     =
DFU_PATH     = C:\usr\Develop\avr\dfu-programmer-win-0.7.0

# Default target
all:

# Write
write:
	$(DFU_PATH)/dfu-programmer.exe $(MCU) flash --force $(TARGET).hex
	
erase:
	$(DFU_PATH)/dfu-programmer.exe $(MCU) erase

# Include LUFA-specific DMBS extension modules
DMBS_LUFA_PATH = $(LUFA_PATH)/Build/LUFA
include $(DMBS_LUFA_PATH)/lufa-sources.mk
include $(DMBS_LUFA_PATH)/lufa-gcc.mk

# Include common DMBS build system modules
DMBS_PATH      = $(LUFA_PATH)/Build/DMBS/DMBS
include $(DMBS_PATH)/core.mk
include $(DMBS_PATH)/cppcheck.mk
include $(DMBS_PATH)/doxygen.mk
include $(DMBS_PATH)/dfu.mk
include $(DMBS_PATH)/gcc.mk
include $(DMBS_PATH)/hid.mk
include $(DMBS_PATH)/avrdude.mk
include $(DMBS_PATH)/atprogram.mk
