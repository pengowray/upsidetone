using LENet;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace upSidetone.Sound {

    public class SymbolQueue {

        //private KeyerState State;
        private SymbolItem[]? SoundQueued = null; // immutable?

        public int Length {
            get {
                return SoundQueued?.Length ?? 0;
            }
        }

        public SymbolQueue() {
            SoundQueued = null;
        }

        public SymbolQueue(SymbolItem[]? soundQueued) {
            SoundQueued = soundQueued;
        }

        public IEnumerable<SymbolItem> Items() {
            if (SoundQueued != null) {
                return SoundQueued.AsEnumerable();
            }
            return Enumerable.Empty<SymbolItem>();
        }
        //public SymbolQueue(SymbolItem[]? soundQueued, KeyerState state) {
        //    SoundQueued = soundQueued;
        //    State = state;
        //}


        /// <summary>
        /// May adjusts the time on item so it's played in the future to avoid overlap with the last item.
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public SymbolQueue AddNext(SymbolItem item) {
            // assume cleared done etc already

            var last = SoundQueued?.LastOrDefault();
            if (last == null) {
                return AddItem(item);

            } else {
                long? doneTime = last.WhenWillBeDone();
                if (doneTime.HasValue && item.Timestamp < doneTime) {
                    item.Timestamp = doneTime.Value;
                }
                return AddItem(item);
            }
        }
        public SymbolQueue AddItem(SymbolItem item) {
            if (SoundQueued != null) {
                //TODO: sort?
                return new SymbolQueue(SoundQueued.Append(item).ToArray());
            } else {
                return new SymbolQueue(new SymbolItem[] { item } );
            }
        }

        public SymbolQueue ClearDoneAndQueued(long timestamp, bool KeepBFill) {
            if (SoundQueued == null) return this;

            SymbolState[] KeepStates = new SymbolState[] { SymbolState.QueuedReq, SymbolState.Playing, SymbolState.PlayingIndef, SymbolState.PostPause };
            if (KeepBFill) {
                List<SymbolItem> newList = new List<SymbolItem>();
                bool keepNext = false;
                foreach (var s in SoundQueued) {
                    var state = s.StateAt(timestamp);
                    if (KeepStates.Contains(state)) {
                        newList.Add(s);
                        keepNext = s.Required;
                    } else if (keepNext && state != SymbolState.None) {
                        newList.Add(s);
                        keepNext = false;
                    } else {
                        keepNext = false;
                    }
                }
                return new SymbolQueue(newList.ToArray());

            } else {
                // same as above but doesn't keep "B Fill" (item after required, for iambic B)
                return new SymbolQueue(SoundQueued?.Where(s => KeepStates.Contains(s.StateAt(timestamp))).ToArray());
            }
        }

        public SymbolQueue ClearDone(long timestamp) {
            return new SymbolQueue(SoundQueued?.Where(s => !s.IsDoneAt(timestamp)).ToArray());
        }

        public long? GetNextTimestamp(long timestamp) {
            var last = SoundQueued?.Last();
            if (last == null) {
                return timestamp;
            }

            var state = last.StateAt(timestamp);

            if (state == SymbolState.Done || state == SymbolState.None) {
                //TODO: maybe allow if needed? (get last that's not done/none)
                throw new InvalidOperationException($"Bad state: {state}. Call ClearDone() first.");
            }

            if (state == SymbolState.PlayingIndef) {
                // you need to stop it first
                return null;
            }

            //if (state == SymbolState.Playing || state == SymbolState.PostPause || state == SymbolState.Queued || state == SymbolState.QueuedReq) {
                //TODO: calc gap if dynamic
                return last.Timestamp + (last?.Length ?? 0) + (last?.Gap ?? 0);
            //}

            //Debug.Assert(false, "Should be unreachable I think");
            //return timestamp;
        }



    }
}
