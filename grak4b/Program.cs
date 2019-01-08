using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Timers;

namespace Grak4b
{
    public class Program
    {   //main plus 3 in job system architecture - mrGrak 2019

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
        
        //let's job-ify all the system work, cause why not?
        public abstract class Job
        {   //create a base class for our 'jobs' to Work()
            public Boolean ready = true;
            public virtual void Work(int startIndex, int endIndex) { }
        }

        public class CollisionJob : Job
        {   
            public override void Work(int startIndex, int endIndex)
            {
                ready = false;
                int i, a;
                int total = endIndex - startIndex;
                for (i = startIndex; i < total; i++)
                {
                    if (Pool.Actives_Read[i]) //find active actor
                    {   //step 2 - check collisions, handle active status
                        for (a = 0; a < Pool.size; a++) //loop all actors,
                        {   //remove any overlapping active actors from main loop
                            if (Pool.Actives_Read[a]) //only check overlaps vs actives
                            {
                                if (i != a) //dont check actor against self
                                {
                                    if (//actor must match x, y pos to be overlapping
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
                ready = true;
            }
        }

        public class MoveJob : Job
        {
            public override void Work(int startIndex, int endIndex)
            {
                ready = false;
                int i;
                int total = endIndex - startIndex;
                for (i = startIndex; i < total; i++)
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
                ready = true;
            }
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

            //create a list of jobs to eval
            List<Job> Jobs = new List<Job>();
            Jobs.Add(new MoveJob());
            Jobs.Add(new CollisionJob());
            Jobs.Add(new CollisionJob());

            Stopwatch timer = Stopwatch.StartNew();
            while (ActiveActors > 1) //one or less actor ends sim
            {   //count this as a frame of work
                Elapsed_Frames++;
                //debug (main only) testing of system
                //Jobs[0].Work(0, Pool.size); Jobs[1].Work(0, Pool.size); Pool.Sync();
                //main +2 thread testing
                if (Jobs[0].ready && Jobs[1].ready && Jobs[2].ready)
                {	//note this is imperative eval until threads start
                    Pool.Sync();
                    ThreadPool.QueueUserWorkItem(a => Jobs[0].Work(0, Pool.size));
                    ThreadPool.QueueUserWorkItem(a => Jobs[1].Work(0, Pool.size/2));
                    ThreadPool.QueueUserWorkItem(a => Jobs[2].Work(Pool.size/2, Pool.size));
                }
                //debug info for sim 
                //Console.WriteLine("frame:" + Elapsed_Frames + " activeActors:" + ActiveActors);
            }
            timer.Stop();
            Console.WriteLine("Grak4b");
            Console.WriteLine("total sim took " + timer.ElapsedMilliseconds + "ms");
        }
    }
}