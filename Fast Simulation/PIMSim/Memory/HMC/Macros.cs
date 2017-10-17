using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public static class Macros
    {

        public static int HMC_MAJOR_VERSION = 3;



        public static int HMC_MINOR_VERSION = 0;


        /* -------------------------------------------- VENDOR ID DATA */

        public static UInt64 HMC_VENDOR_ID = 0xF;



        public static int HMC_PRODUCT_REVISION = 0x2;



        public static int HMC_PROTOCOL_REVISION = 0x2;



        public static int HMC_PHY_REVISION = 0x1;


        /* -------------------------------------------- PHYSICAL MACRO DATA */

        public static UInt64 HMC_PHY_SPEED = 0x0;


        /* -------------------------------------------- RETURN CODES */
        public static int HMC_ERROR_PARAMS = -3;
        public static int HMC_ERROR_DEV_INIT = -2;
        public static int HMC_ERROR = -1;
        public static int HMC_OK = 0;
        public static int HMC_STALL = 2;

        /* -------------------------------------------- TRACE VALUES */
        public static uint HMC_TRACE_BANK = 0x0001;
        public static uint HMC_TRACE_QUEUE = 0x0002;
        public static uint HMC_TRACE_CMD = 0x0004;
        public static uint HMC_TRACE_STALL = 0x0008;
        public static uint HMC_TRACE_LATENCY = 0x0010;
        public static uint HMC_TRACE_POWER = 0x0020;

        /* -------------------------------------------- MACROS */
        public static int HMC_MAX_DEVS = 8;
        public static int HMC_MAX_LINKS = 8;
        public static int HMC_MIN_LINKS = 4;
        public static int HMC_MAX_CAPACITY = 8;
        public static int HMC_MIN_CAPACITY = 4;
        public static int HMC_MAX_VAULTS = 64;
        public static int HMC_MIN_VAULTS = 32;
        public static int HMC_MAX_BANKS = 32;
        public static int HMC_MIN_BANKS = 8;
        public static int HMC_MIN_DRAMS = 20;
        public static int HMC_MAX_DRAMS = 20;
        public static int HMC_MIN_QUEUE_DEPTH = 2;
        public static Int64 HMC_MAX_QUEUE_DEPTH = 65536;
        public static int HMC_MAX_UQ_PACKET = 34;

        public static int HMC_MAX_CMC = 70;

        public static uint HMC_RQST_INVALID = 0;
        public static uint HMC_RQST_VALID = 1;
        public static int HMC_RQST_CONFLICT = 2;
        public static uint HMC_RQST_STALLED = 3;
        public static int HMC_RQST_NEW = 4;
        public static uint HMC_RQST_ZOMBIE = 13;

        public static Int64 HMC_1GB = 1073741824;

        public static int HMC_NUM_REGS = 26;

        public const UInt64 HMC_REG_EDR0 = 0x2B0000;
        public const UInt64 HMC_REG_EDR1 = 0x2B0001;
        public const UInt64 HMC_REG_EDR2 = 0x2B0002;
        public const UInt64 HMC_REG_EDR3 = 0x2B0003;
        public const UInt64 HMC_REG_ERR = 0x2B0004;
        public const UInt64 HMC_REG_GC = 0x280000;
        public const UInt64 HMC_REG_LC0 = 0x240000;
        public const UInt64 HMC_REG_LC1 = 0x250000;
        public const UInt64 HMC_REG_LC2 = 0x260000;
        public const UInt64 HMC_REG_LC3 = 0x270000;
        public const UInt64 HMC_REG_LRLL0 = 0x240003;
        public const UInt64 HMC_REG_LRLL1 = 0x250003;
        public const UInt64 HMC_REG_LRLL2 = 0x260003;
        public const UInt64 HMC_REG_LRLL3 = 0x270003;
        public const UInt64 HMC_REG_LR0 = 0x0C0000;
        public const UInt64 HMC_REG_LR1 = 0x0D0000;
        public const UInt64 HMC_REG_LR2 = 0x0E0000;
        public const UInt64 HMC_REG_LR3 = 0x0F0000;
        public const UInt64 HMC_REG_IBTC0 = 0x040000;
        public const UInt64 HMC_REG_IBTC1 = 0x050000;
        public const UInt64 HMC_REG_IBTC2 = 0x060000;
        public const UInt64 HMC_REG_IBTC3 = 0x070000;
        public const UInt64 HMC_REG_AC = 0x2C0000;
        public const UInt64 HMC_REG_VCR = 0x108000;
        public const UInt64 HMC_REG_FEAT = 0x2C0003;
        public const UInt64 HMC_REG_RVID = 0x2C0004;

        public static int HMC_REG_EDR0_IDX = 0x000000;
        public static int HMC_REG_EDR1_IDX = 0x000001;
        public static int HMC_REG_EDR2_IDX = 0x000002;
        public static int HMC_REG_EDR3_IDX = 0x000003;
        public static int HMC_REG_ERR_IDX = 0x000004;
        public static int HMC_REG_GC_IDX = 0x000005;
        public static int HMC_REG_LC0_IDX = 0x000006;
        public static int HMC_REG_LC1_IDX = 0x000007;
        public static int HMC_REG_LC2_IDX = 0x000008;
        public static int HMC_REG_LC3_IDX = 0x000009;
        public static int HMC_REG_LRLL0_IDX = 0x00000A;
        public static int HMC_REG_LRLL1_IDX = 0x00000B;
        public static int HMC_REG_LRLL2_IDX = 0x00000C;
        public static int HMC_REG_LRLL3_IDX = 0x00000D;
        public static int HMC_REG_LR0_IDX = 0x00000E;
        public static int HMC_REG_LR1_IDX = 0x00000F;
        public static int HMC_REG_LR2_IDX = 0x000010;
        public static int HMC_REG_LR3_IDX = 0x000011;
        public static int HMC_REG_IBTC0_IDX = 0x000012;
        public static int HMC_REG_IBTC1_IDX = 0x000013;
        public static int HMC_REG_IBTC2_IDX = 0x000014;
        public static int HMC_REG_IBTC3_IDX = 0x000015;
        public static int HMC_REG_AC_IDX = 0x000016;
        public static int HMC_REG_VCR_IDX = 0x000017;
        public static int HMC_REG_FEAT_IDX = 0x000018;
        public static int HMC_REG_RVID_IDX = 0x000019;

        public static float HMC_MILLIWATT_TO_BTU = 0.003414f;
        public static uint HMC_DEF_DRAM_LATENCY = 2;

    }
}
