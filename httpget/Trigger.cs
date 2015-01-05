using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace httpget
{
    public class Trigger
    {
        public delegate void OnTrigger();

        //method to be triggered
        OnTrigger triggeredMethod;

        //interval between triggers
        int interval;
        //counter denoting time since last trigger
        int counter = 0;

        //flag if tyhe counter is running
        bool running;

        /// <param name="interval">Time between triggers in ms.</param>
        /// <param name="triggeredMethod">The method to ber triggered.</param>
        public Trigger(int interval, OnTrigger triggeredMethod)
        {
            this.interval = interval;
            this.triggeredMethod = triggeredMethod;
        }

        /// <summary>
        /// Ticks the trigger counter
        /// </summary>
        /// <param name="elapsedMilliseconds">Elapsed time since the last tick.</param>
        public void tick(int elapsedMilliseconds)
        {
            counter += elapsedMilliseconds;
            if (counter >= interval)
            {
                counter -= interval;
                triggeredMethod();
            }
        }

        /// <summary>
        /// Starts the trigger counter.
        /// </summary>
        public void Start()
        {
            running = true;
        }

        /// <summary>
        /// Stops the trigger counter.
        /// </summary>
        public void Stop()
        {
            running = false;
        }

        /// <summary>
        /// Stops the trigger counter and resets it to zero.
        /// </summary>
        public void Reset()
        {
            Stop();
            counter = 0;
        }

        /// <summary>
        /// Gets if the counter is running.
        /// </summary>
        public bool Running
        {
            get { return running; }
        }

        public void SetInterval(int interval)
        {
            this.interval = interval;
            counter = 0;
        }
    }
}
