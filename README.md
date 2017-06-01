# PIMSim

1.	About PIMSim
PIMSim is a trace-based simulator of PIM architecture, which provides both host-side and memory-side simulation. Besides, PIMSim provides high degree of freedom of PIM unit configuration which can be defined by users. PIMSim provides three input types to adapt different simulation demanding. PIMSim aims to support new memory types such as HMC, HBM et. 

2.  Getting started with PIMSim

2.1  Prepare input traces.
PIMSim is a trace-based simulator. It supports three different types of inputs. The trace filename should be “ CPUx.trace ” (x indicate the id of CPU). For each trace line, it should provide cycle, instruction, [read/write], [Data], [Address].

      0|ld t1, SS:[rsp]|R|D=0x0000000000000001 A=0x7fffffffee20
      1|addi rsp, rsp, 0x8
      2|ld t1, SS:[rsp]|R|D=0x0000000000000001 A=0x7fffffffee20
      4|mov rdx, rdx, rsp
      5|limm t1, 0xfffffffffffffff0
      6|and rsp, rsp, t1
      7|st rax, SS:[rsp + 0xfffffffffffffff8]|W|D=0x0000000000000000 A=0x7fffffffee18

                        
You can use “PIM_Operation” to indicate that this instruction should be executed in memory-side units, like this :

      26|PIM_st r14, SS:[rsp + 0xffffff8]|W|D=0x0000000000000000 A=0x7fffffffedf8

If you want to nominate a snippet of code to be executed in memory-side, you can label traces like this:

      PIM_BLOCK_START
      18|rdip t7, %ctrl153, 
      19|st t7, SS:[rsp + 0xffffffffff8]|W|D=0x00000000004001ba A=0x7fffffffee08
      20|subi rsp, rsp, 0x8
      21|wrip , t7, t1
      22|st r15, SS:[rsp + 0xfffffffff8]|W|D=0x0000000000000000 A=0x7fffffffee00
      PIM_BLOCK_END

If you just want to insert a function, you can add this:

      3|Function_Add_Start  ; cycle/function name
        input = 0x7fffffffee20	;input address
        input = 0x7fffffffee18	; input address
        output = 0x5807b0		;output address
        latency = 2		;operation latency without data fetching 
      Function_Add_End                      
                       
This input indicates an function named 'Add', which has two inputs and one output. The operation duration lasts 2 cycles.

2.2 Prepare Config Files
PIMSim has two kind of config inputs: PIM settings and RAM settings. For PIM settings, you can attach them by PIMConfig Class in "/Configs/PIMConfigs.cs". For RAM settings, we modified and integrate HMCSim ( https://github.com/tactcomplabs/gc64-hmcsim ) and DRAMSim2 ( https://github.com/dramninjasUMD/DRAMSim2 ) to adapt PIM Simulation. If you need simuate HMC or DRAM (or both of them),  you should provide config files of them. In the future, we'll add more new memory simuations such as NVM, Memristor and so on.


2.3 Run PIMSim

You can run PIMSim by providing such paramaters:

      $PIMSim -t tracefilepath -c configfilepath -n numproc -o outputfile
        -t, -trace_path FILEPATH      specify the path folder of input trace.
        -c, -config_path FILEPATH     specify the path folder of input configs.
        -o, -output  FILENAME         specify the file name of output file.
        -n, -N  PROCCOUNT         specify the count of host proc.
