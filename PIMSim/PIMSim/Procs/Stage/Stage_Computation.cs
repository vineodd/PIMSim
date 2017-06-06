#region Reference

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplePIM.General;

#endregion

namespace SimplePIM.Procs
{
    /// <summary>
    /// Computing Stage
    /// </summary>
    public class Stage_Computing : Stage
    {
        #region Private Variables

        private Counter add;
        private Counter multi;
        private int add_ = 0;
        private int multi_ = 0;

        #endregion

        #region Public Methods

        /// <summary>
        /// set counter
        /// </summary>
        /// <param name="multi_"></param>
        public void set_multi_counter(ref Counter multi_)
        {
            multi = multi_;
        }


        /// <summary>
        /// set counter
        /// </summary>
        /// <param name="add_"></param>
        public void set_add_counter(ref Counter add_)
        {
            add = add_;
        }

        /// <summary>
        /// set stage input
        /// </summary>
        /// <param name="obj"></param>
        public override void set_input(object obj)
        {
            if (last != null)
            {
                if (last[0].output_ready)
                {
                    var obj_ = new Instruction() as object;
                    last[0].get_output(ref obj_);
                    input = obj_ as Instruction;
                    input_ready = true;
                }
            }
        }

        /// <summary>
        /// internal step
        /// </summary>
        /// <returns></returns>
        public override bool Step()
        {

            if (add_ != 0 || multi_ != 0)
            {
                int count_multi = 0;
                int count_add = 0;
                while (true)
                {
                    bool res = true;
                    int tp = add_;
                    for (int i = 0; i < tp; i++)
                    {
                        res = add.WaitOne();
                        if (res)
                            add_--;
                        count_add++;
                    }
                    if (!res)
                    {
                        for (int i = 0; i < count_add; i++)
                            add.Release();

                    }
                    tp = multi_;
                    bool res_ = true;
                    for (int i = 0; i < tp; i++)
                    {
                        res_ = multi.WaitOne();
                        if (res_)
                            multi_--;
                        count_multi++;
                    }
                    if (!res_)
                    {
                        for (int i = 0; i < count_multi; i++)
                            multi.Release();
                    }
                    if (res && res_)
                    {

                        write_output();
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                //first time the ins been handled
                set_input(null);
                if (!read_input())
                    return true;
                if ((intermid as Instruction).type == InstructionType.NOP)
                {
                    write_output();
                    return true;
                }
                string op = (intermid as Instruction).Operation;

                if (Cal_table.ContainOPs(op))
                {
                    //found entry
                    var item = Cal_table.GetItem(op);
                    add_ = item.Item2;
                    multi_ = item.Item3;

                }
                else
                {
                    add_ = 1;
                    multi_ = 0;
                }
                int count_multi = 0;
                int count_add = 0;
                while (true)
                {
                    bool res = true;
                    int tp = add_;
                    for (int i = 0; i < tp; i++)
                    {
                        res = add.WaitOne();
                        if (res)
                            add_--;
                        count_add++;
                    }
                    if (!res)
                    {
                        for (int i = 0; i < count_add; i++)
                            add.Release();

                    }
                    tp = multi_;
                    bool res_ = true;
                    for (int i = 0; i < tp; i++)
                    {
                        res_ = multi.WaitOne();
                        if (res_)
                            multi_--;
                        count_multi++;
                    }
                    if (!res_)
                    {
                        for (int i = 0; i < count_multi; i++)
                            multi.Release();
                    }
                    if (res && res_)
                    {
                        write_output();
                        return true;
                    }
                    return false;
                }

            }
        }


        /// <summary>
        /// read input
        /// </summary>
        /// <returns></returns>
        public override bool read_input()
        {
            if (input_ready && input != null)
            {
                intermid = input;
                input_ready = false;
                input = null;
                return true;
            }
            return false;
        }

        #endregion
    }
}
