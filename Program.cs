using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SFML;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
namespace Chip8
{

    public class Chip8
    {
        public byte[] ram = new byte[4096];
        public byte[] regs = new byte[16];
        public int pc;
        public byte stackPointer;
        public ushort[] stack = new ushort[16];
        //timer registers and timers here later
        public byte delayTimer, soundTimer;
        public int regI;
        public byte[,] display = new byte[64, 32];
        public int waitingForInputKey = -1;
        public bool waitingForInput = false;
        public bool[] inputs = new bool[16];
        public bool displayUpdated = false;
        public Chip8(byte[] programCode)
        {
            byte[] font =
           {
              0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
              0x20, 0x60, 0x20, 0x20, 0x70, // 1
              0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
              0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
              0x90, 0x90, 0xF0, 0x10, 0x10, // 4
              0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
              0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
              0xF0, 0x10, 0x20, 0x40, 0x40, // 7
              0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
              0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
              0xF0, 0x90, 0xF0, 0x90, 0x90, // A
              0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
              0xF0, 0x80, 0x80, 0x80, 0xF0, // C
              0xE0, 0x90, 0x90, 0x90, 0xE0, // D
              0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
              0xF0, 0x80, 0xF0, 0x80, 0x80  // F
            };
            for (int i = 0; i < font.Length; i++)
            {
                ram[i] = font[i];
            }

            for (int i = 0x200; i < 0xFFF; i++)
            {
                if ((i - 0x200) >= programCode.Length)
                    break;
                ram[i] = programCode[i - 0x200];
            }
            pc = 0x200;
        }

        public void Execute()
        {
            //while (delayTimer > 0)
            if (delayTimer > 0)
                delayTimer--;
            if (soundTimer > 0)
                soundTimer--;
            byte lowbyte = ram[pc + 1];
            byte highbyte = ram[pc];
            ushort currentRegister = BitConverter.ToUInt16(new byte[] { lowbyte, highbyte }, 0);
            int nnn = currentRegister & (0b0000111111111111);
            int n = currentRegister & (0b0000000000001111);
            int x = (currentRegister & (0b0000111100000000)) >> 8;
            int y = (currentRegister & (0b0000000011110000)) >> 4;
            int insType1 = currentRegister & (0b1111000000000000);//highbyte  & (0b11110000);
            int insType2 = currentRegister & (0b0000000000001111);
            byte kk = ram[pc + 1];
            //  Console.Write("PC: " + pc + " ");
            if (currentRegister == 0x00E0)
            {
                for (int x_ = 0; x_ < 64; x_++)
                    for (int y_ = 0; y_ < 32; y_++)
                    {
                        display[x_, y_] = 0;
                    }
                pc += 2;
            }
            else if (currentRegister == 0x00EE)
            {
                pc = stack[stackPointer];
                stackPointer--;
            }
            else
                switch (insType1)
                {
                    case 0x1000:
                        //jump
                        pc = nnn;
                        break;
                    case 0x2000:
                        //call 
                        stackPointer++;
                        stack[stackPointer] = (ushort)(pc + 2);

                        pc = nnn;
                        break;
                    case 0x3000:
                        // Console.WriteLine("SE V" + x + " " + kk);
                        if (regs[x] == kk)
                            pc += 2;
                        pc += 2;
                        break;
                    case 0x4000:
                        // Console.WriteLine("SNE V" + x + " " + kk);
                        if (regs[x] != kk)
                            pc += 2;
                        pc += 2;
                        break;
                    case 0x5000:
                        // Console.WriteLine("SE V" + x + " V" + y);
                        if (regs[x] == regs[y])
                            pc += 2;
                        pc += 2;
                        break;
                    case 0x6000:
                        // Console.WriteLine("LD V" + x + " " + kk);
                        regs[x] = kk;
                        pc += 2;
                        break;
                    case 0x7000:
                        //Console.WriteLine("ADD V" + x + " " + kk);
                        regs[x] += kk;
                        pc += 2;
                        break;
                    case 0x8000:
                        switch (insType2)
                        {
                            case 0:
                                regs[x] = regs[y];
                                pc += 2;
                                break;
                            case 1:
                                //Console.WriteLine("OR  V" + x + " V" + y);
                                regs[x] |= regs[y]; pc += 2;
                                break;
                            case 2:
                                //Console.WriteLine("AND V" + x + " V" + y);
                                regs[x] &= regs[y]; pc += 2;
                                break;
                            case 3:
                                //Console.WriteLine("XOR V" + x + " V" + y);
                                regs[x] ^= regs[y]; pc += 2;
                                break;
                            case 4:
                                // Console.WriteLine("ADD V" + x + " V" + y);
                                byte added = (byte)(regs[x] + regs[y]);
                                if (regs[x] + regs[y] > 255)
                                    regs[0xF] = 1;
                                else regs[0xF] = 0;
                                regs[x] = added;
                                pc += 2;
                                break;
                            case 5:
                                //Console.WriteLine("SUB V" + x + " V" + y);
                                if (regs[x] > regs[y])
                                    regs[0xF] = 1;
                                else regs[0xF] = 0;
                                regs[x] -= regs[y];
                                pc += 2;
                                break;
                            case 6:
                                //Console.WriteLine("SHR V" + x + " V" + y);
                                //set VF based on leastsignificant bit, weird shit
                                if ((regs[x] & 1) == 1)
                                    regs[0xF] = 1;
                                else regs[0xF] = 0;
                                regs[x] /= 2;
                                pc += 2;
                                break;
                            case 7:
                                //Console.WriteLine("SUBN V" + x + " V" + y);
                                if (regs[x] < regs[y])
                                    regs[0xF] = 1;
                                else regs[0xF] = 0;

                                byte test = regs[y];
                                test -= regs[x];
                                regs[x] = test; //i couldnt cast simply (regs[y] - regs[x]) for some reason lol
                                pc += 2;
                                break;
                            case 0xE:
                                //Console.WriteLine("SHL V" + x + " V" + y);
                                if ((regs[x] >> 7) == 1)
                                    regs[0xF] = 1;
                                else regs[0xF] = 0;

                                regs[x] *= 2;
                                pc += 2;
                                break;
                        }
                        break;
                    case 0x9000:

                        //Console.WriteLine("SNE V" + x + " V" + y);
                        if (regs[x] != regs[y])
                            pc += 2;
                        pc += 2;

                        break;
                    case 0xA000:
                        //Console.WriteLine("LD I " + nnn);
                        regI = nnn;
                        pc += 2;
                        break;
                    case 0xB000:

                        //   Console.WriteLine("JP V0 " + nnn);
                        pc = regs[0] + nnn;
                        pc += 2;


                        break;
                    case 0xC000:
                        {
                            //Console.WriteLine("RND V" + x + " " + kk);
                            Random rand = new Random();
                            regs[x] = (byte)(rand.Next(255) & kk);
                            pc += 2;
                        }
                        break;
                    case 0xD000:
                        {
                            //Console.WriteLine("DRW V" + x + " V" + y + " " + n);
                            regs[0xF] = 0;
                            for (int _y = 0; _y < n; _y++)
                            {
                                byte sprByte = ram[regI + _y];
                                for (int _x = 0; _x < 8; _x++)
                                {
                                    if (display[(_x + regs[x]) % 64, (_y + regs[y]) % 32] == 1)
                                        regs[0xF] = 1;
                                    if ((sprByte & (0x80 >> _x)) != 0)
                                    {
                                        display[(_x + regs[x]) % 64, (_y + regs[y]) % 32] ^= 1;
                                    }
                                }
                            }
                            displayUpdated = true;

                            pc += 2;
                        }
                        break;
                    case 0xE000:
                        switch (lowbyte)
                        {
                            case 0x9E:
                                {
                                    //Console.WriteLine("SKP V" + x);

                                    if (inputs[regs[x] - 1])
                                        pc += 2;
                                    pc += 2;
                                }
                                break;
                            case 0xA1:

                                // Console.WriteLine("SKNP V" + x);
                                if (!inputs[regs[x] - 1])
                                    pc += 2;
                                pc += 2;
                                break;
                        }
                        break;
                    case 0xF000:
                        switch (lowbyte)
                        {
                            case 0x07:
                                regs[x] = delayTimer;
                                pc += 2;
                                break;
                            case 0x0A: //UNIMPLEMENTED
                                       //Console.WriteLine("LD V" + x + " K");
                                bool pressed = false;
                                for (int i = 0; i < 16; i++)
                                {
                                    if (inputs[i] == true)
                                    {
                                        regs[x] = (byte)i;
                                        pressed = true;
                                    }
                                }
                                if (!pressed)
                                    return;
                                pc += 2;
                                break;
                            case 0x15:
                                //    Console.WriteLine("LD DT, V" + x);
                                delayTimer = regs[x];
                                pc += 2;
                                break;
                            case 0x18:
                                //Console.WriteLine("LD ST, V" + x);
                                soundTimer = regs[x];
                                pc += 2;
                                break;
                            case 0x1E:
                                //Console.WriteLine("ADD I, V" + x);
                                regI += regs[x];
                                pc += 2;
                                break;
                            case 0x29:
                                //Console.WriteLine("LD F, V" + x);
                                regI = regs[x] * 5;
                                pc += 2;
                                break;
                            case 0x33:
                                //Console.WriteLine("LD B, V" + x);
                                ram[regI + 0] = (byte)((regs[x] / 100));
                                ram[regI + 1] = (byte)((regs[x] / 10) % 10);
                                ram[regI + 2] = (byte)(regs[x] % 10);
                                pc += 2;
                                break;
                            case 0x55:
                                //Console.WriteLine("LD [I], V" + x);
                                for (int i = 0; i < x + 1; i++)
                                    ram[regI + i] = regs[i];
                                // regI += x + 1;
                                pc += 2;
                                break;
                            case 0x65:
                                //Console.WriteLine("LD V" + x + " [I]");
                                for (int i = 0; i < x + 1; i++)
                                    regs[i] = ram[regI + i];
                                //regI += x + 1;
                                pc += 2;
                                break;
                        }
                        break;
                    default:
                        Console.WriteLine("unknown: " + currentRegister.ToString("X"));
                        break;
                }


            //Thread.Sleep(1200);

        }

        public void PrintDisassembly()
        {
            while (pc < 0xFFF)
            {


                byte lowbyte = ram[pc + 1];
                byte highbyte = ram[pc];
                short currentRegister = BitConverter.ToInt16(new byte[] { lowbyte, highbyte }, 0);

                int nnn = currentRegister & (0b0000111111111111);
                int n = currentRegister & (0b0000000000001111);
                int x = (currentRegister & (0b0000111100000000)) >> 8;
                int y = (currentRegister & (0b0000000011110000)) >> 4;
                int insType1 = currentRegister & (0b1111000000000000);//highbyte  & (0b11110000);
                int insType2 = currentRegister & (0b0000000000001111);
                byte kk = ram[pc + 1];
                Console.Write("PC: " + pc + " ");
                if (currentRegister == 0x00E0)
                    Console.WriteLine("CLS");
                else if (currentRegister == 0x00EE)
                    Console.WriteLine("RET");
                else if (insType1 == 0x1000)
                    Console.WriteLine("JP " + nnn);
                else if (insType1 == 0x2000)
                    Console.WriteLine("CALL " + nnn);
                else if (insType1 == 0x3000)
                    Console.WriteLine("SE V" + x + " " + kk);
                else if (insType1 == 0x4000)
                    Console.WriteLine("SNE V" + x + " " + kk);
                else if (insType1 == 0x5000)
                    Console.WriteLine("SE V" + x + " V" + y);
                else if (insType1 == 0x6000)
                    Console.WriteLine("LD V" + x + " " + kk);
                else if (insType1 == 0x7000)
                    Console.WriteLine("ADD V" + x + " " + kk);
                else if (insType1 == 0x8000 && insType2 == 0)
                    Console.WriteLine("LD  V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 1)
                    Console.WriteLine("OR  V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 2)
                    Console.WriteLine("AND V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 3)
                    Console.WriteLine("XOR V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 4)
                    Console.WriteLine("ADD V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 5)
                    Console.WriteLine("SUB V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 6)
                    Console.WriteLine("SHR V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 7)
                    Console.WriteLine("SUBN V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 0xE)
                    Console.WriteLine("SHL V" + x + " V" + y);
                else if (insType1 == 0x9000 && insType2 == 0)
                    Console.WriteLine("SNE V" + x + " V" + y);
                else if (insType1 == 0xA000)
                    Console.WriteLine("LD I " + nnn);
                else if (insType1 == 0xB000)
                    Console.WriteLine("JP V0 " + nnn);
                else if (insType1 == 0xC000)
                    Console.WriteLine("RND V" + x + " " + kk);
                else if (insType1 == 0xD000)
                    Console.WriteLine("DRW V" + x + " V" + y + " " + n);
                else if (insType1 == 0xE000 && lowbyte == 0x9E)
                    Console.WriteLine("SKP V" + x);
                else if (insType1 == 0xE000 && lowbyte == 0xA1)
                    Console.WriteLine("SKNP V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x07)
                    Console.WriteLine("LD V" + x + " DT");
                else if (insType1 == 0xF000 && lowbyte == 0x0A)
                    Console.WriteLine("LD V" + x + " K");
                else if (insType1 == 0xF000 && lowbyte == 0x15)
                    Console.WriteLine("LD DT, V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x18)
                    Console.WriteLine("LD ST, V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x1E)
                    Console.WriteLine("ADD I, V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x29)
                    Console.WriteLine("LD F, V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x33)
                    Console.WriteLine("LD B, V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x55)
                    Console.WriteLine("LD [I], V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x65)
                    Console.WriteLine("LD V" + x + " [I]");
                else
                    Console.WriteLine("unknown: " + currentRegister.ToString("X"));
                pc += 2;
            }
        }

        public List<string> GetCurrentLine()
        {
            List<string> lines = new List<string>();
            for (int i = 0; i < 2 * 5; i += 2)
            {
                byte lowbyte = ram[(pc + 1) + i];
                byte highbyte = ram[(pc) + i];
                short currentRegister = BitConverter.ToInt16(new byte[] { lowbyte, highbyte }, 0);

                int nnn = currentRegister & (0b0000111111111111);
                int n = currentRegister & (0b0000000000001111);
                int x = (currentRegister & (0b0000111100000000)) >> 8;
                int y = (currentRegister & (0b0000000011110000)) >> 4;
                int insType1 = currentRegister & (0b1111000000000000);//highbyte  & (0b11110000);
                int insType2 = currentRegister & (0b0000000000001111);
                byte kk = ram[(pc + 1) + i];
                //Console.Write("PC: " + pc + " ");
                if (currentRegister == 0x00E0)
                    lines.Add("CLS");
                else if (currentRegister == 0x00EE)
                    lines.Add("RET");
                else if (insType1 == 0x1000)
                    lines.Add("JP " + nnn);
                else if (insType1 == 0x2000)
                    lines.Add("CALL " + nnn);
                else if (insType1 == 0x3000)
                    lines.Add("SE V" + x + " " + kk);
                else if (insType1 == 0x4000)
                    lines.Add("SNE V" + x + " " + kk);
                else if (insType1 == 0x5000)
                    lines.Add("SE V" + x + " V" + y);
                else if (insType1 == 0x6000)
                    lines.Add("LD V" + x + " " + kk);
                else if (insType1 == 0x7000)
                    lines.Add("ADD V" + x + " " + kk);
                else if (insType1 == 0x8000 && insType2 == 0)
                    lines.Add("LD  V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 1)
                    lines.Add("OR  V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 2)
                    lines.Add("AND V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 3)
                    lines.Add("XOR V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 4)
                    lines.Add("ADD V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 5)
                    lines.Add("SUB V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 6)
                    lines.Add("SHR V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 7)
                    lines.Add("SUBN V" + x + " V" + y);
                else if (insType1 == 0x8000 && insType2 == 0xE)
                    lines.Add("SHL V" + x + " V" + y);
                else if (insType1 == 0x9000 && insType2 == 0)
                    lines.Add("SNE V" + x + " V" + y);
                else if (insType1 == 0xA000)
                    lines.Add("LD I " + nnn);
                else if (insType1 == 0xB000)
                    lines.Add("JP V0 " + nnn);
                else if (insType1 == 0xC000)
                    lines.Add("RND V" + x + " " + kk);
                else if (insType1 == 0xD000)
                    lines.Add("DRW V" + x + " V" + y + " " + n);
                else if (insType1 == 0xE000 && lowbyte == 0x9E)
                    lines.Add("SKP V" + x);
                else if (insType1 == 0xE000 && lowbyte == 0xA1)
                    lines.Add("SKNP V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x07)
                    lines.Add("LD V" + x + " DT");
                else if (insType1 == 0xF000 && lowbyte == 0x0A)
                    lines.Add("LD V" + x + " K");
                else if (insType1 == 0xF000 && lowbyte == 0x15)
                    lines.Add("LD DT, V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x18)
                    lines.Add("LD ST, V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x1E)
                    lines.Add("ADD I, V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x29)
                    lines.Add("LD F, V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x33)
                    lines.Add("LD B, V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x55)
                    lines.Add("LD [I], V" + x);
                else if (insType1 == 0xF000 && lowbyte == 0x65)
                    lines.Add("LD V" + x + " [I]");
                else
                    lines.Add("unknown: " + currentRegister.ToString("X"));
            }
            return lines;
        }

    }

    class MainClass
    {
        static void OnClose(object sender, EventArgs e)
        {
            // Close the window when OnClose event is received
            RenderWindow window = (RenderWindow)sender;
            window.Close();
        }
        public static Chip8 chip8;



        public static void OnKeyPressed(object sender, SFML.Window.KeyEventArgs e)
        {
            for (int i = (int)Keyboard.Key.Num0; i < (int)Keyboard.Key.Escape; i++)
            {
                if (e.Code == (Keyboard.Key)i)
                {
                    //if(chip8.waitingForInput == 0)
                    chip8.inputs[i - (int)Keyboard.Key.Num0] = true;
                }
                // else chip8.inputs[i - (int)Keyboard.Key.Num0] = false;
            }
        }

        public static void OnKeyReleased(object sender, SFML.Window.KeyEventArgs e)
        {
            for (int i = (int)Keyboard.Key.Num0; i < (int)Keyboard.Key.Escape; i++)
            {
                if (e.Code == (Keyboard.Key)i)
                {

                    chip8.inputs[i - (int)Keyboard.Key.Num0] = false;
                }
                // else chip8.inputs[i - (int)Keyboard.Key.Num0] = false;
            }
        }

        public static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");

            //byte[] loadedCode = File.ReadAllBytes("pong.rom");
            byte[] loadedCode = File.ReadAllBytes("INVADERS");
            //byte[] loadedCode = File.ReadAllBytes("test_opcode.ch8");
            //byte[] loadedCode = File.ReadAllBytes("BC_test.ch8");
            chip8 = new Chip8(loadedCode);
            RenderWindow app = new RenderWindow(new VideoMode(800, 600), "chip8 emu");
            app.Closed += new EventHandler(OnClose);

            float TILESIZEX = (float)app.Size.X / 64;
            float TILESIZEY = (float)app.Size.Y / 32;
            RectangleShape shape = new RectangleShape(new Vector2f(TILESIZEX, TILESIZEY));

            app.KeyPressed += OnKeyPressed;
            app.KeyReleased += OnKeyReleased;

            Font font = new Font("Arial.ttf");
            Text text = new Text("", font);
            //app.KeyReleased += 
            // Start the game loop
            //app.SetFramerateLimit(60);
            //app.SetVerticalSyncEnabled(true);
            while (app.IsOpen)
            {
                chip8.Execute();
                // Process events
                app.DispatchEvents();

                //if (chip8.displayUpdated)
                {
                    // Clear screen
                    app.Clear();

                    for (int y = 0; y < 32; y++)
                    {
                        for (int x = 0; x < 64; x++)
                        {
                            shape.Position = new Vector2f(x * TILESIZEX, y * TILESIZEY);
                            if (chip8.display[x, y] == 1)
                                shape.FillColor = Color.Blue;
                            else shape.FillColor = Color.Black;
                            app.Draw(shape);
                        }
                        //Console.WriteLine();
                    }

                    List<string> lines = chip8.GetCurrentLine();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        text.DisplayedString = lines[i];
                        text.Position = new Vector2f(0, i * 25);
                        app.Draw(text);

                    }

                    for (int i = 0; i < 16; i++)
                    {
                        text.DisplayedString = chip8.inputs[i] ? "1" : "0";
                        text.Position = new Vector2f((i % 4) * 25 + 200, (i / 4) * 25);
                        app.Draw(text);
                    }
                    app.Display();
                    chip8.displayUpdated = false;
                }

                //for (int i = 0; i < 16; i++)
                //  chip8.inputs[i] = false;

                // Draw the sprite


                // Draw the string

                // Sleep to slow down emulation speed


            }
        }
    }
}
