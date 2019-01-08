using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Timers;

namespace Grak1
{
    public class Program
    {   //single threaded design - mrGrak 2019
        public class Actor
        {   //needs to be less than 64bytes to flow well thru L1 cache
            public float x = 0;
            public float y = 0;
            public AiType ai_type = AiType.Wait;
            public Boolean active = false;
        }
        public enum AiType : byte { Wait, Move }
        public static class Pool //build the lists that model actors
        {
            public static int size = 256;
            public static List<Actor> Actors = new List<Actor>();
            static Pool() //constructor for this global class
            {   //setup the pools to handle size amount of actors
                for (int i = 0; i < size; i++) { Actors.Add(new Actor()); }
            }
        }
        public static int GoalPosition_X = 256; //top right of game grid
        public static int GoalPosition_Y = 0; //actors move towards goal pos x,y
        public static double Elapsed_Frames = 0;
        public static void Main(string[] args)
        {   //setup actors in pool
            int i;
            for(i = 0; i < Pool.size; i++)
            {   //set active, position actors from 0,0 down-right
                Pool.Actors[i].active = true;
                Pool.Actors[i].x = i; //position diagonally right and
                Pool.Actors[i].y = i * 2; //down, with some space between
                if (i % 2 != 0) //make half actors diff aiType
                { Pool.Actors[i].ai_type = AiType.Move; }
            }
            int activeActors = Pool.size; //setup active counter
            int a; //collision checking counter
            Stopwatch timer = Stopwatch.StartNew();
            while (activeActors > 1) //one or less actor ends sim
            {
                Elapsed_Frames++; //count this as a frame of work
                for (i = 0; i < Pool.size; i++) //main work loop
                {   
                    if(Pool.Actors[i].active) //only process active actors
                    {
                        //step 1 - move actors based on ai enum
                        if (Pool.Actors[i].ai_type == AiType.Move)
                        {   //move towards x goal position
                            if (Pool.Actors[i].x < GoalPosition_X)
                            { Pool.Actors[i].x += 0.5f; }
                            else { Pool.Actors[i].x -= 0.5f; }
                            //move towards y goal position
                            if (Pool.Actors[i].y < GoalPosition_Y)
                            { Pool.Actors[i].y += 0.5f; }
                            else { Pool.Actors[i].y -= 0.5f; }
                            //tell actor to wait next frame
                            Pool.Actors[i].ai_type = AiType.Wait;
                        }
                        else { Pool.Actors[i].ai_type = AiType.Move; }
                        //step 2 - check collisions, handle active status
                        for (a = 0; a < Pool.size; a++) //loop all actors,
                        {   //remove any overlapping active actors from main loop
                            if(Pool.Actors[a].active) //only check overlaps vs actives
                            {
                                if(i != a) //dont check actor against self
                                {
                                    if  ( //actor must match x, y pos to be overlapping
                                        (Pool.Actors[a].x == Pool.Actors[i].x)
                                        &&
                                        (Pool.Actors[a].y == Pool.Actors[i].y)
                                        )
                                    {   //both actors are removed from main loop
                                        Pool.Actors[a].active = false;
                                        Pool.Actors[i].active = false;
                                        activeActors -= 2;
                                    }
                                }
                            }
                        } //actor work complete
                    }
                }
            }
            timer.Stop();
            Console.WriteLine("Grak1");
            Console.WriteLine("Frames: " + Elapsed_Frames);
            Console.WriteLine("total sim took " + timer.ElapsedMilliseconds + "ms");
        }
    }
}