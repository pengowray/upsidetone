using System;
using System.Windows.Media.Animation;

namespace upSidetone.Sound {

    public enum SymbolState {
        // TODO: buffer state (locked-into-buffer; done-buffering) vs play state vs adsr state (e.g. if don't want to count part of attack or release as the note)

        None,
        Queued, // not played yet
        QueuedReq, // not played yet and required to play
        Playing,
        PlayingIndef, // playing an indefinite note (straight key down)
        PostPause,
        Done
    }

    public class SymbolItem {
        public long Timestamp;
        public long? Length; 
        public long? Gap; // todo: option to snap to next gap length (3)
        public int Seq; // number in sequence within letter (may effect tone or weight in special modes)

        public LeverKind Lever; // maybe not needed?
        //public Requiredness Requiredness;
        public bool Required = false;
        public bool IambicBMode = false;
        public SwitchableDelayedSound? Sound;

        public SymbolItem() { }

        public SymbolItem(long timestamp, long? length, long? gap, LeverKind lever, bool required, SwitchableDelayedSound? sound) {
            Timestamp = timestamp;
            Length = length;
            Gap = gap;
            Lever = lever;
            Required = required;
        }

        public void SetLever(LeverKind lever) {
            Lever = lever;
            
        }

        public SymbolState StateAt(long timestamp) {
            if (Lever == LeverKind.None)
                return SymbolState.None;

            if (timestamp < Timestamp)
                return Required ? SymbolState.QueuedReq : SymbolState.Queued;

            if (IsIndefinite()) 
                return SymbolState.PlayingIndef;

            if (timestamp < Timestamp + Length.Value) // Length can't be null
                return SymbolState.Playing;

            if (Gap.HasValue && timestamp < Timestamp + Length.Value + Gap)
                return SymbolState.PostPause;

            return SymbolState.Done;
        }

        public bool IsDoneAt(long timestamp) {
            // todo: optimize
            var state = StateAt(timestamp);
            return state == SymbolState.Done || state == SymbolState.None;
        }

        public long? WhenWillBeDone() {
            //if (Timestamp == null) return null;

            return Timestamp + (Length ?? 0) + (Gap ?? 0);
        }

        public bool IsIndefinite() {
            if (Lever == LeverKind.None) return false;

            //if (lever == LeverKind.Straight || lever == LeverKind.Oscillate) return true;
            if (Length == null) return true;
            return false;
        }

    }
}
