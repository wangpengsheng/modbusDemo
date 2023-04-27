//创建一个保存NModbus数据的文件
using System;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using NModbus;
using NModbus.Device;
namespace ModbusTest
{
    public class ModbusHelper
    {
        private readonly static TcpClient _tcpClient = new("127.0.0.1", 502);
        private readonly static IModbusMaster _master;
        //保持寄存器的数据
        public static readonly ushort[] _holdingRegisters = new ushort[501];
        //输入寄存器的数据
        public static readonly ushort[] _inputRegisters = new ushort[501];
        //初始化保持寄存器的数据
        static ModbusHelper()
        {
            //清除保持寄存器的数据
            Array.Clear(_holdingRegisters, 0, _holdingRegisters.Length);
            //清除输入寄存器的数据
            Array.Clear(_inputRegisters, 0, _inputRegisters.Length);
            var factory = new NModbus.ModbusFactory();
            _master = factory.CreateMaster(_tcpClient);
            _master.Transport.ReadTimeout = 500;
            _master.Transport.WriteTimeout = 500;
            _master.Transport.Retries = 3;
            _master.Transport.WaitToRetryMilliseconds = 500;

        }
        //通过TCP进行通信读取远端Slave的数据来填充保持寄存器的数据
        public static void FillHoldingRegisters()
        {

            for (ushort i = 1; i < _holdingRegisters.Length; i += 100)
            {
                var registers = _master.ReadHoldingRegisters(1, i, 100);
                // var inputRegisters = master.ReadInputRegisters(1, i, 100);
                //使用一句话将registers的数据填充到_holdingRegisters中
                Array.Copy(registers, 0, _holdingRegisters, i, registers.Length);
                // Array.Copy(inputRegisters, 0, _inputRegisters, i, inputRegisters.Length);
            }
        }

        //使用一个Task来定时更新保持寄存器的数据
        public static void FillHoldingRegistersAsync()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                while (true)
                {
                    var start = DateTime.Now;
                    FillHoldingRegisters();
                    var end = DateTime.Now;
                    var time = end - start;
                    Console.WriteLine($"耗时{time.TotalMilliseconds}");
                    System.Threading.Thread.Sleep(200);
                }
            });
        }

        //将数据刷新到控制台
        public static void RefreshConsole()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                while (true)
                {
                    Console.WriteLine();
                    Console.WriteLine("保持寄存器的数据");
                    for (int i = 0; i < _holdingRegisters.Length; i++)
                    {
                        Console.Write($"{_holdingRegisters[i]}\t");
                        if ((i + 1) % 10 == 0)
                        {
                            Console.WriteLine();
                        }
                    }
                    System.Threading.Thread.Sleep(200);
                }
            });
        }

        public static async Task<bool> Subscribe()
        {
            while (true)
            {
                if (_holdingRegisters[100] == 1)
                {
                    var result = await Task.Run(() =>
                    {
                        return Task.FromResult(true);
                    });
                    if (result)
                    {
                        System.Console.WriteLine("执行订阅成功...");
                        _master.WriteMultipleRegisters(1, 100, new ushort[] { 0 });
                    }
                    else
                    {
                        System.Console.WriteLine("执行订阅失败...");
                    }
                }
                await Task.Delay(100);
            }
        }
    }


    public class ModbusTest
    {
        public static void Main()
        {
            ModbusHelper.FillHoldingRegistersAsync();
            ModbusHelper.RefreshConsole();
            ModbusHelper.Subscribe();
            Console.ReadLine();
        }
    };
}