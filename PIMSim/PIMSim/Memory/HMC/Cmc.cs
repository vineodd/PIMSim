using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIMSim.Memory.HMC
{
    public class hmc_cmc
    {

        /* -- data */
        public hmc_rqst type;                /*! HMC-SIM: HMC_CMC_T: REGISTERED REQUEST TYPE */
        public UInt32 cmd;                   /*! HMC-SIM: HMC_CMC_T: COMMAND CODE OF THE REQUEST */
        public UInt32 rqst_len;              /*! HMC-SIM: HMC_CMC_T: REQUEST LENGTH */
        public UInt32 rsp_len;               /*! HMC-SIM: HMC_CMC_T: RESPONSE LENGTH */
        public hmc_response rsp_cmd;         /*! HMC-SIM: HMC_CMC_T: RESPONSE COMMAND */
        public uint rsp_cmd_code;           /*! HMC-SIM: HMC_CMC_T: RESPONSE COMMAND CODE */

        public int track_power;                /*! HMC-SIM: HMC_CMC_T: DOES THIS CMC OP TRACK POWER? */

        public UInt32 active;                /*! HMC-SIM: HMC_CMC_T: SIGNALS THAT THE COMMAND IS ACTIVE */
       

        /* -- fptrs */
        //public int (*cmc_register)(hmc_rqst_t*,  UInt32*,  UInt32*,  UInt32*,   hmc_response_t*,   uint*){
        //    }
        public delegate int Callback_cmc_register(List<hmc_rqst> T1, List<UInt32> T2, List<UInt32> T3, List<UInt32> T4, List<hmc_response> T5, List<uint> T7);
        //int (*cmc_execute)(void*,      /* hmc */
        //                   UInt32,    /* dev */
        //                   UInt32,    /* quad */
        //                   UInt32,    /* vault */
        //                   UInt32,    /* bank */
        //                   UInt64,    /* addr */
        //                   UInt32,    /* length */
        //                   UInt64,    /* head */
        //                   UInt64,    /* tail */
        //                   UInt64*,  /* rqst_payload */
        //                   UInt64*); /* rsp_payload */
        public delegate int Callback_cmc_execute(

       
                           UInt32 T2,    /* dev */
                           UInt32 T3,    /* quad */
                           UInt32 T4,    /* vault */
                           UInt32 T5,    /* bank */
                           UInt64 T6,    /* addr */
                           UInt32 T7,    /* length */
                           UInt64 T8,    /* head */
                           UInt64 T9,    /* tail */
                           UInt64[] T10,  /* rqst_payload */
                           UInt64[] T11); /* rsp_payload */


        //void (*cmc_str)(char*);
        public delegate void Callback_cmc_str(ref string T1);
        //void (*cmc_power)(UInt32*,   /* row_ops */
        //                  float*);     /* transient power */
        public delegate void Callback_cmc_power(ref UInt32 T1, ref float T2);

        public Callback_cmc_register cmc_register;
        public Callback_cmc_execute cmc_execute;
        public Callback_cmc_power cmc_power;
        public Callback_cmc_str cmc_str;
    }

    public enum hmc_rqst
    {
        WR16,               /*! HMC-SIM: HMC_RQST_T: 16-BYTE WRITE REQUEST */
        WR32,               /*! HMC-SIM: HMC_RQST_T: 32-BYTE WRITE REQUEST */
        WR48,               /*! HMC-SIM: HMC_RQST_T: 48-BYTE WRITE REQUEST */
        WR64,               /*! HMC-SIM: HMC_RQST_T: 64-BYTE WRITE REQUEST */
        WR80,               /*! HMC-SIM: HMC_RQST_T: 80-BYTE WRITE REQUEST */
        WR96,               /*! HMC-SIM: HMC_RQST_T: 96-BYTE WRITE REQUEST */
        WR112,              /*! HMC-SIM: HMC_RQST_T: 112-BYTE WRITE REQUEST */
        WR128,              /*! HMC-SIM: HMC_RQST_T: 128-BYTE WRITE REQUEST */
        MD_WR,              /*! HMC-SIM: HMC_RQST_T: MODE WRITE REQUEST */
        BWR,                /*! HMC-SIM: HMC_RQST_T: BIT WRITE REQUEST */
        TWOADD8,            /*! HMC-SIM: HMC_RQST_T: DUAL 8-byte ADD IMMEDIATE */
        ADD16,              /*! HMC-SIM: HMC_RQST_T: SINGLE 16-byte ADD IMMEDIATE */
        P_WR16,             /*! HMC-SIM: HMC_RQST_T: 16-BYTE POSTED WRITE REQUEST */
        P_WR32,             /*! HMC-SIM: HMC_RQST_T: 32-BYTE POSTED WRITE REQUEST */
        P_WR48,             /*! HMC-SIM: HMC_RQST_T: 48-BYTE POSTED WRITE REQUEST */
        P_WR64,             /*! HMC-SIM: HMC_RQST_T: 64-BYTE POSTED WRITE REQUEST */
        P_WR80,             /*! HMC-SIM: HMC_RQST_T: 80-BYTE POSTED WRITE REQUEST */
        P_WR96,             /*! HMC-SIM: HMC_RQST_T: 96-BYTE POSTED WRITE REQUEST */
        P_WR112,            /*! HMC-SIM: HMC_RQST_T: 112-BYTE POSTED WRITE REQUEST */
        P_WR128,            /*! HMC-SIM: HMC_RQST_T: 128-BYTE POSTED WRITE REQUEST */
        P_BWR,              /*! HMC-SIM: HMC_RQST_T: POSTED BIT WRITE REQUEST */
        P_2ADD8,            /*! HMC-SIM: HMC_RQST_T: POSTED DUAL 8-BYTE ADD IMMEDIATE */
        P_ADD16,            /*! HMC-SIM: HMC_RQST_T: POSTED SINGLE 16-BYTE ADD IMMEDIATE */
        RD16,               /*! HMC-SIM: HMC_RQST_T: 16-BYTE READ REQUEST */
        RD32,               /*! HMC-SIM: HMC_RQST_T: 32-BYTE READ REQUEST */
        RD48,               /*! HMC-SIM: HMC_RQST_T: 48-BYTE READ REQUEST */
        RD64,               /*! HMC-SIM: HMC_RQST_T: 64-BYTE READ REQUEST */
        RD80,               /*! HMC-SIM: HMC_RQST_T: 80-BYTE READ REQUEST */
        RD96,               /*! HMC-SIM: HMC_RQST_T: 96-BYTE READ REQUEST */
        RD112,              /*! HMC-SIM: HMC_RQST_T: 112-BYTE READ REQUEST */
        RD128,              /*! HMC-SIM: HMC_RQST_T: 128-BYTE READ REQUEST */
        RD256,                          /*! HMC-SIM: HMC_RQST_T: 256-BYTE READ REQUEST */
        MD_RD,              /*! HMC-SIM: HMC_RQST_T: MODE READ REQUEST */
        FLOW_NULL,          /*! HMC-SIM: HMC_RQST_T: NULL FLOW CONTROL */
        PRET,               /*! HMC-SIM: HMC_RQST_T: RETRY POINTER RETURN FLOW CONTROL */
        TRET,               /*! HMC-SIM: HMC_RQST_T: TOKEN RETURN FLOW CONTROL */
        IRTRY,              /*! HMC-SIM: HMC_RQST_T: INIT RETRY FLOW CONTROL */

        /* -- version 2.0 Command Additions */
        WR256,              /*! HMC-SIM: HMC_RQST_T: 256-BYTE WRITE REQUEST */
        P_WR256,            /*! HMC-SIM: HMC_RQST_T: 256-BYTE POSTED WRITE REQUEST */
        TWOADDS8R,          /*! HMC-SIM: HMC_RQST_T: */
        ADDS16R,            /*! HMC-SIM: HMC_RQST_T: */
        INC8,               /*! HMC-SIM: HMC_RQST_T: 8-BYTE ATOMIC INCREMENT */
        P_INC8,             /*! HMC-SIM: HMC_RQST_T: POSTED 8-BYTE ATOMIC INCREMENT */
        XOR16,              /*! HMC-SIM: HMC_RQST_T: 16-BYTE ATOMIC XOR */
        OR16,               /*! HMC-SIM: HMC_RQST_T: 16-BYTE ATOMIC OR */
        NOR16,              /*! HMC-SIM: HMC_RQST_T: 16-BYTE ATOMIC NOR */
        AND16,              /*! HMC-SIM: HMC_RQST_T: 16-BYTE ATOMIC AND */
        NAND16,             /*! HMC-SIM: HMC_RQST_T: 16-BYTE ATOMIC NAND */
        CASGT8,             /*! HMC-SIM: HMC_RQST_T: 8-BYTE COMPARE AND SWAP IF GT */
        CASGT16,            /*! HMC-SIM: HMC_RQST_T: 16-BYTE COMPARE AND SWAP IF GT */
        CASLT8,             /*! HMC-SIM: HMC_RQST_T: 8-BYTE COMPARE AND SWAP IF LT */
        CASLT16,            /*! HMC-SIM: HMC_RQST_T: 16-BYTE COMPARE AND SWAP IF LT */
        CASEQ8,             /*! HMC-SIM: HMC_RQST_T: 8-BYTE COMPARE AND SWAP IF EQ */
        CASZERO16,          /*! HMC-SIM: HMC_RQST_T: 16-BYTE COMPARE AND SWAP IF ZERO */
        EQ8,                /*! HMC-SIM: HMC_RQST_T: 8-BYTE ATOMIC EQUAL */
        EQ16,               /*! HMC-SIM: HMC_RQST_T: 16-BYTE ATOMIC EQUAL */
        BWR8R,              /*! HMC-SIM: HMC_RQST_T: 8-BYTE ATOMIC BIT WRITE WITH RETURN */
        SWAP16,             /*! HMC-SIM: HMC_RQST_T: 16-BYTE ATOMIC SWAP */

        /* -- CMC Types */
        CMC04,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=4 */
        CMC05,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=5 */
        CMC06,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=6 */
        CMC07,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=7 */
        CMC20,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=20 */
        CMC21,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=21 */
        CMC22,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=22 */
        CMC23,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=23 */
        CMC32,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=32 */
        CMC36,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=36 */
        CMC37,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=37 */
        CMC38,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=38 */
        CMC39,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=39 */
        CMC41,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=41 */
        CMC42,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=42 */
        CMC43,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=43 */
        CMC44,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=44 */
        CMC45,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=45 */
        CMC46,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=46 */
        CMC47,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=47 */
        CMC56,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=56 */
        CMC57,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=57 */
        CMC58,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=58 */
        CMC59,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=59 */
        CMC60,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=60 */
        CMC61,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=61 */
        CMC62,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=62 */
        CMC63,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=63 */
        CMC69,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=69 */
        CMC70,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=70 */
        CMC71,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=71 */
        CMC72,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=72 */
        CMC73,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=73 */
        CMC74,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=74 */
        CMC75,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=75 */
        CMC76,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=76 */
        CMC77,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=77 */
        CMC78,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=78 */
        CMC85,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=85 */
        CMC86,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=86 */
        CMC87,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=87 */
        CMC88,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=88 */
        CMC89,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=89 */
        CMC90,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=90 */
        CMC91,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=91 */
        CMC92,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=92 */
        CMC93,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=93 */
        CMC94,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=94 */
        CMC102,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=102 */
        CMC103,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=103 */
        CMC107,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=107 */
        CMC108,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=108 */
        CMC109,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=109 */
        CMC110,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=110 */
        CMC111,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=111 */
        CMC112,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=112 */
        CMC113,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=113 */
        CMC114,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=114 */
        CMC115,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=115 */
        CMC116,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=116 */
        CMC117,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=117 */
        CMC118,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=118 */
        CMC120,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=120 */
        CMC121,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=121 */
        CMC122,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=122 */
        CMC123,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=123 */
        CMC124,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=124 */
        CMC125,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=125 */
        CMC126,                         /*! HMC-SIM: HMC_RQST_T: CMC CMD=126 */
        CMC127,                          /*! HMC-SIM: HMC_RQST_T: CMC CMD=127 */
    }
    public enum hmc_response
    {
        RD_RS,              /*! HMC-SIM: HMC_RESPONSE_T: READ RESPONSE */
        WR_RS,              /*! HMC-SIM: HMC_RESPONSE_T: WRITE RESPONSE */
        MD_RD_RS,           /*! HMC-SIM: HMC_RESPONSE_T: MODE READ RESPONSE */
        MD_WR_RS,           /*! HMC-SIM: HMC_RESPONSE_T: MODE WRITE RESPONSE */
        RSP_ERROR,          /*! HMC-SIM: HMC_RESPONSE_T: ERROR RESPONSE */
        RSP_NONE,			/*! HMC-SIM: HMC_RESPONSE_T: NO RESPONSE COMMAND */
        RSP_CMC                         /*! HMC-SIM: HMC_RESPONSE_T: CUSTOM CMC RESPONSE */
    }
    public class cmc_table
    {
       public hmc_rqst type;
       public UInt32 cmd;
       public UInt32 idx;
        public cmc_table(hmc_rqst t1, UInt32 t2, UInt32 t3)
        {
            type = t1;
            cmd = t2;
            idx = t3;
        }
    }
    public static class Ctable
    {
        public static readonly cmc_table[] ctable = {
            new cmc_table(hmc_rqst.CMC04,4,0),
            new cmc_table(hmc_rqst.CMC05,5,1),
  new cmc_table(hmc_rqst.CMC06,6,2),
  new cmc_table(hmc_rqst.CMC07,7,3),
  new cmc_table(hmc_rqst.CMC20,20,4),
  new cmc_table(hmc_rqst.CMC21,21,5),
  new cmc_table(hmc_rqst.CMC22,22,6),
  new cmc_table(hmc_rqst.CMC23,23,7),
  new cmc_table(hmc_rqst.CMC32,32,8),
  new cmc_table(hmc_rqst.CMC36,36,9),
  new cmc_table(hmc_rqst.CMC37,37,10),
  new cmc_table(hmc_rqst.CMC38,38,11),
  new cmc_table(hmc_rqst.CMC39,39,12),
  new cmc_table(hmc_rqst.CMC41,41,13),
  new cmc_table(hmc_rqst.CMC42,42,14),
  new cmc_table(hmc_rqst.CMC43,43,15),
  new cmc_table(hmc_rqst.CMC44,44,16),
  new cmc_table(hmc_rqst.CMC45,45,17),
  new cmc_table(hmc_rqst.CMC46,46,18),
  new cmc_table(hmc_rqst.CMC47,47,19),
  new cmc_table(hmc_rqst.CMC56,56,20),
  new cmc_table(hmc_rqst.CMC57,57,21),
  new cmc_table(hmc_rqst.CMC58,58,22),
  new cmc_table(hmc_rqst.CMC59,59,23),
  new cmc_table(hmc_rqst.CMC60,60,24),
  new cmc_table(hmc_rqst.CMC61,61,25),
  new cmc_table(hmc_rqst.CMC62,62,26),
  new cmc_table(hmc_rqst.CMC63,63,27),
  new cmc_table(hmc_rqst.CMC69,69,28),
  new cmc_table(hmc_rqst.CMC70,70,29),
  new cmc_table(hmc_rqst.CMC71,71,30),
  new cmc_table(hmc_rqst.CMC72,72,31),
  new cmc_table(hmc_rqst.CMC73,73,32),
  new cmc_table(hmc_rqst.CMC74,74,33),
  new cmc_table(hmc_rqst.CMC75,75,34),
  new cmc_table(hmc_rqst.CMC76,76,35),
  new cmc_table(hmc_rqst.CMC77,77,36),
  new cmc_table(hmc_rqst.CMC78,78,37),
  new cmc_table(hmc_rqst.CMC85,85,38),
  new cmc_table(hmc_rqst.CMC86,86,39),
  new cmc_table(hmc_rqst.CMC87,87,40),
  new cmc_table(hmc_rqst.CMC88,88,41),
  new cmc_table(hmc_rqst.CMC89,89,42),
  new cmc_table(hmc_rqst.CMC90,90,43),
  new cmc_table(hmc_rqst.CMC91,91,44),
  new cmc_table(hmc_rqst.CMC92,92,45),
  new cmc_table(hmc_rqst.CMC93,93,46),
  new cmc_table(hmc_rqst.CMC94,94,47),
  new cmc_table(hmc_rqst.CMC102,102,48),
  new cmc_table(hmc_rqst.CMC103,103,49),
  new cmc_table(hmc_rqst.CMC107,107,50),
  new cmc_table(hmc_rqst.CMC108,108,51),
  new cmc_table(hmc_rqst.CMC109,109,52),
  new cmc_table(hmc_rqst.CMC110,110,53),
  new cmc_table(hmc_rqst.CMC111,111,54),
  new cmc_table(hmc_rqst.CMC112,112,55),
  new cmc_table(hmc_rqst.CMC113,113,56),
  new cmc_table(hmc_rqst.CMC114,114,57),
  new cmc_table(hmc_rqst.CMC115,115,58),
  new cmc_table(hmc_rqst.CMC116,116,59),
  new cmc_table(hmc_rqst.CMC117,117,60),
  new cmc_table(hmc_rqst.CMC118,118,61),
  new cmc_table(hmc_rqst.CMC120,120,62),
  new cmc_table(hmc_rqst.CMC121,121,63),
  new cmc_table(hmc_rqst.CMC122,122,64),
  new cmc_table(hmc_rqst.CMC123,123,65),
  new cmc_table(hmc_rqst.CMC124,124,66),
  new cmc_table(hmc_rqst.CMC125,125,67),
  new cmc_table(hmc_rqst.CMC126,126,68),
  new cmc_table(hmc_rqst.CMC127,127,69)


        };
    }

}
