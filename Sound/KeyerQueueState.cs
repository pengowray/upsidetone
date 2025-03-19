namespace upSidetone.Sound {
    public class KeyerQueueState {
        //public static KeyerState DefaultState = new KeyerState(); // needs to be immutable

        public FillState? FillState = null;
        public LeverKind RequireNext = LeverKind.None; // until it's queued
        public LeverKind StraightDown = LeverKind.None; // None or StraightDown or Oscillate. Is the straight key down? (shouldn't we just ask Levers?)

        public SymbolQueue? SymbolQueue = null;

        public long? GetNextTimestamp(long timestamp) {
            if (SymbolQueue == null) {
                return timestamp;
            }
            return SymbolQueue.GetNextTimestamp(timestamp);
        }
        public KeyerQueueState ShallowClone() {
            return new KeyerQueueState() {
                FillState = this.FillState,
                RequireNext = this.RequireNext,
                StraightDown = this.StraightDown,
                SymbolQueue = this.SymbolQueue
            };
        }
        public KeyerQueueState DeeperClone() {
            // not a deep clone tho
            return new KeyerQueueState() {
                FillState = this.FillState?.ShallowClone() ?? null,
                RequireNext = this.RequireNext,
                StraightDown = this.StraightDown,
                SymbolQueue = this.SymbolQueue
            };
        }


        public KeyerQueueState ClearDoneAndQueued(long timestamp) {
            var newQueue = SymbolQueue?.ClearDoneAndQueued(timestamp, FillState?.BFill ?? false);
            var newState = ShallowClone();
            newState.SymbolQueue = newQueue;
            return newState;
        }

        public void AddNextInPlace(SymbolItem item) {
            SymbolQueue = SymbolQueue?.AddNext(item) ?? new SymbolQueue().AddNext(item);
        }

    }
}
