
#ifndef __PIM_KERNEL__
#define __PIM_KERNEL__

#include "mem/mem_object.hh"
#include "params/PIMKernel.hh"
#include "mem/port.hh"
#include <vector>
#include <string.h>
#include "sim/eventq.hh"
#include <pthread.h>
#include "mem/qport.hh"
#include "debug/PIM.hh"
#include "mem/packet.hh"

using namespace std;

//  @PIM PIMKernel is a base class to perform in-memory calculation.
class PIMKernel : public MemObject
{
    
public:
    // current status of the kernel
    enum Status{
        Idle,	// the kernel is power-on but idle. When Idle, no address and result are stored in the kernel.
	Ready,	// host-side processors had sent pim operations to this PIM kernel.
        WaitingResp,	// the kernel had successfully sent a PIM read/write to the memory. 
	SendRetry,	// the kernel failded to send a memory request and needs to retry sending.
        Finish,	// the PIM computing is complete.
        Poweroff,	// the kernel is power-off.
    };

    // current status of the registers
    enum DataStatus{
        dataEmpty,	// initial status
	dataReady,	// the address is given
        dataWaitingResp,	// the read/write packet to the address is sent but not response
        dataFinish	// the result is read/written
    };
    
    typedef uint64_t dataType;

protected:
    // basic master port to operate data in the memory
    class TimingPIMPort : public MasterPort
    {
      public:

        TimingPIMPort(const std::string& _name, PIMKernel* _kernel)
            : MasterPort(_name, _kernel), kernel(_kernel),
              retryRespEvent([this]{ sendRetryResp(); }, name())
        { }

      protected:

        PIMKernel* kernel;

        struct TickEvent : public Event
        {
            PacketPtr pkt;
            PIMKernel *kernel;

            TickEvent(PIMKernel *_kernel) : pkt(NULL), kernel(_kernel) {}
            const char *description() const { return "PIMKernel tick"; }
            void schedule(PacketPtr _pkt, Tick t);
        };

        EventFunctionWrapper retryRespEvent;
    };
    
    // PIM master port
    // used for sent memory requests
    class PIMMasterPort : public TimingPIMPort
    {
      public:

        PIMMasterPort(PIMKernel *_kernel)
            : TimingPIMPort(_kernel->name() + ".master_port", _kernel),
              tickEvent(_kernel)
        { }

      protected:

        virtual bool recvTimingResp(PacketPtr pkt);

        virtual void recvReqRetry();

        struct PIMTickEvent : public TickEvent
        {

            PIMTickEvent(PIMKernel *_kernel)
                : TickEvent(_kernel) {}
            void process();
            const char *description() const { return "PIMKernel tick"; }
        };

        PIMTickEvent tickEvent;

    };

    // PIM slave port
    // used for receiving memory response
    class RecvPIMPort : public QueuedSlavePort
    {

        RespPacketQueue queue;
        PIMKernel& kernel;

      public:

        RecvPIMPort(const std::string& name, PIMKernel& _kernel);

      protected:

        Tick recvAtomic(PacketPtr pkt);

        void recvFunctional(PacketPtr pkt);

        bool recvTimingReq(PacketPtr);

        virtual AddrRangeList getAddrRanges() const;

    };

    PIMMasterPort port;
    RecvPIMPort mem_port;

public:
    // current status
    Status status;

    typedef PIMKernelParams Params;

    PIMKernel(const Params *p);

protected:

    const Params* parms;

    // PIM register defination
    typedef PIMKernel::dataType Regs;
    std::vector<pair<Regs,DataStatus>> regs;

    // all data are stored here
    std::vector<Regs> data;


    std::vector<Packet::PIMSenderState *> pendingPIMqueue;
    
protected:

    EventFunctionWrapper tickEvent;
    EventFunctionWrapper computeEvent;
    EventFunctionWrapper finishEvent;

    
public:

    virtual ~PIMKernel();
    
    int _id;

    int _latency;

    int _input;

    int _output;

    Addr pim_addr_base;
     
    Tick tickid;


public:
    void regStats() override;


    
    Stats::Scalar recv_pim_commands;
    Stats::Scalar exec_pim_commands;
    Stats::Scalar failed_pim_commands;
    Stats::Scalar sent_pim_commands;
    Stats::Scalar computing_counts;

    Stats::Scalar read_packets;
    Stats::Scalar write_packets;
    Stats::Scalar read_retry;
    Stats::Scalar write_retry;
    Stats::Scalar retry_failed;
    Stats::Scalar active_cycle;
    Stats::Scalar retry_cycle;

    PacketPtr retry_pkt;

public:

    bool getReadyStatus();

    virtual bool inputReady();
    virtual bool outputReady();
    virtual void tick();
    virtual void finish();
    virtual void trycompute();
    const AddrRangeList addrRanges;

    virtual bool start(PacketPtr pkt);
    virtual void active();
    virtual bool powerOff();
    virtual bool isActive();
    virtual bool canModify();
    virtual bool needSchedule();

    virtual int locateLatest();
    virtual bool readReg(int index);
    virtual bool doDataCallback(PacketPtr pkt, Tick response_time);

    std::vector<PacketPtr> recPackets;

    virtual BaseMasterPort &getMasterPort(const std::string &if_name, PortID idx = InvalidPortID) override;
    virtual BaseSlavePort& getSlavePort(const std::string& if_name,
                                        PortID idx = InvalidPortID) override;
    void init() override;

    virtual dataType doCompute();

protected:

    Tick recvAtomicSnoop(PacketPtr pkt){
	fatal("PIM cannot run at atomic mode");
    };

    virtual Tick recvAtomic(PacketPtr pkt);
    virtual void functionalAccess(PacketPtr pkt);
    virtual bool recvTimingResp(PacketPtr pkt);
    virtual void recvTimingSnoopReq(PacketPtr pkt);
    virtual bool recvTimingReq(PacketPtr pkt);
    virtual void recvFunctional(PacketPtr pkt);
    virtual void recvReqRetry();

};
#endif // __PIM_KERNEL__

