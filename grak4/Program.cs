using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Timers;

namespace Grak4
{
    public class Program
    {   //main plus three thread design - mrGrak 2019

        //Step2() is naively divided between two threads  in this version
        //this is done for quick testing of this idea
        public enum AiType : byte { Wait, Move }
        public static class Pool //build the lists that model actors
        {
            public static int size = 256;
            //two lists: one for reads, one for writes
            public static List<float> Actor_Xpos_Read = new List<float>();
            public static List<float> Actor_Xpos_Write = new List<float>();
            
            public static List<float> Actor_Ypos_Read = new List<float>();
            public static List<float> Actor_Ypos_Write = new List<float>();

            public static List<AiType> Ai_Read = new List<AiType>();
            public static List<AiType> Ai_Write = new List<AiType>();

            public static List<Boolean> Actives_Read = new List<Boolean>();
            public static List<Boolean> Actives_Write = new List<Boolean>();

            static Pool() //constructor for this global class
            {   //setup the pools to handle size amount of actors
                for (int i = 0; i < size; i++)
                {
                    Actor_Xpos_Read.Add(new float());
                    Actor_Xpos_Write.Add(new float());
                    Actor_Ypos_Read.Add(new float());
                    Actor_Ypos_Write.Add(new float());
                    Ai_Read.Add(new AiType());
                    Ai_Write.Add(new AiType());
                    Actives_Read.Add(new bool());
                    Actives_Write.Add(new bool());
                }
            }
            public static void Sync()
            {   //sync all written data to all lists
                for (int i = 0; i < size; i++)
                {   //these are basic data types, so this is by value (not by ref)
                    Actor_Xpos_Read[i] = Actor_Xpos_Write[i];
                    Actor_Ypos_Read[i] = Actor_Ypos_Write[i];
                    Ai_Read[i] = Ai_Write[i];
                    Actives_Read[i] = Actives_Write[i];
                }
            }
        }

        public static Boolean Step1_Ready = true;
        public static void Step1()
        {
            int i;
            Step1_Ready = false;
            for (i = 0; i < Pool.size; i++)
            {
                if (Pool.Actives_Read[i]) //find active actor
                {   //step 1 - move actors based on ai enum
                    if (Pool.Ai_Read[i] == AiType.Move)
                    {   //move towards x goal position
                        if (Pool.Actor_Xpos_Read[i] < GoalPosition_X)
                        { Pool.Actor_Xpos_Write[i] += 0.5f; }
                        else { Pool.Actor_Xpos_Write[i] -= 0.5f; }
                        //move towards y goal position
                        if (Pool.Actor_Ypos_Read[i] < GoalPosition_Y)
                        { Pool.Actor_Ypos_Write[i] += 0.5f; }
                        else { Pool.Actor_Ypos_Write[i] -= 0.5f; }
                        //tell actor to wait next frame
                        Pool.Ai_Write[i] = AiType.Wait;
                    }
                    else { Pool.Ai_Write[i] = AiType.Move; }
                }
            }
            Step1_Ready = true;
        }

        //first 'half' of pool
        public static Boolean Step2_Ready = true;
        public static void Step2()
        {
            int i, a;
            Step2_Ready = false;
            for (i = 0; i < Pool.size / 2; i++)
            {
                if (Pool.Actives_Read[i]) //find active actor
                {   //step 2 - check collisions, handle active status
                    for (a = 0; a < Pool.size; a++) //loop all actors,
                    {   //remove any overlapping active actors from main loop
                        if (Pool.Actives_Read[a]) //only check overlaps vs actives
                        {
                            if (i != a) //dont check actor against self
                            {
                                if ( //actor must match x, y pos to be overlapping
                                    (Pool.Actor_Xpos_Read[a] == Pool.Actor_Xpos_Read[i])
                                    &&
                                    (Pool.Actor_Ypos_Read[a] == Pool.Actor_Ypos_Read[i])
                                    )
                                {   //both actors are removed from main loop
                                    Pool.Actives_Write[a] = false;
                                    Interlocked.Decrement(ref ActiveActors);
                                    Pool.Actives_Write[i] = false;
                                    Interlocked.Decrement(ref ActiveActors);
                                }
                            }
                        }
                    }
                }
            }
            Step2_Ready = true;
        }

        //second 'half' of pool
        public static Boolean Step3_Ready = true;
        public static void Step3()
        {
            int i, a;
            Step3_Ready = false;
            for (i = Pool.size / 2; i < Pool.size / 2; i++)
            {
                if (Pool.Actives_Read[i]) //find active actor
                {   //step 2 - check collisions, handle active status
                    for (a = 0; a < Pool.size; a++) //loop all actors,
                    {   //remove any overlapping active actors from main loop
                        if (Pool.Actives_Read[a]) //only check overlaps vs actives
                        {
                            if (i != a) //dont check actor against self
                            {
                                if ( //actor must match x, y pos to be overlapping
                                    (Pool.Actor_Xpos_Read[a] == Pool.Actor_Xpos_Read[i])
                                    &&
                                    (Pool.Actor_Ypos_Read[a] == Pool.Actor_Ypos_Read[i])
                                    )
                                {   //both actors are removed from main loop
                                    Pool.Actives_Write[a] = false;
                                    Interlocked.Decrement(ref ActiveActors);
                                    Pool.Actives_Write[i] = false;
                                    Interlocked.Decrement(ref ActiveActors);
                                }
                            }
                        }
                    }
                }
            }
            Step3_Ready = true;
        }

        public static int GoalPosition_X = 256; //top right of game grid
        public static int GoalPosition_Y = 0; //actors move towards goal pos x,y
        public static double Elapsed_Frames = 0;
        public static int ActiveActors = Pool.size;
        public static void Main(string[] args)
        {   //setup actors in pool
            for (int i = 0; i < Pool.size; i++)
            {   //set active, position actors from 0,0 down-right
                Pool.Actives_Write[i] = true;
                Pool.Actor_Xpos_Write[i] = i; //position diagonally right and
                Pool.Actor_Ypos_Write[i] = i * 2; //down, with some space between
                if (i % 2 != 0) //make half actors diff aiType
                { Pool.Ai_Write[i] = AiType.Move; }
                Pool.Sync(); //set write data into read data lists
            }
            Stopwatch timer = Stopwatch.StartNew();
            while (ActiveActors > 1) //one or less actor ends sim
            {   //count this as a frame of work
                Elapsed_Frames++;
                //debug (main only) testing of system
                //Step1(); Step2(); Pool.SyncAndSwap();
                //main +2 thread testing
                if (Step1_Ready && Step2_Ready && Step3_Ready)
                {	//note this is imperative eval until threads start
                    Pool.Sync();
                    ThreadPool.QueueUserWorkItem(a => Step1());
                    ThreadPool.QueueUserWorkItem(a => Step2());
                    ThreadPool.QueueUserWorkItem(a => Step3());
                }
                //debug info for sim 
                //Console.WriteLine("frame:" + Elapsed_Frames + " activeActors:" + ActiveActors);
            }
            timer.Stop();
            Console.WriteLine("Grak4");
            Console.WriteLine("total sim took " + timer.ElapsedMilliseconds + "ms");
        }
    }
}