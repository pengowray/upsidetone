namespace upSidetone.Sound {
    public class FillState { // was: KeyerState
        public LeverKind[]? Fill = null;
        public int FillPos = 0;
        public bool BFill = false; // true: iambic B squeeze mode is on: whenever a symbol is queued, the next in the fill is required (todo: maybe rename to BFillModeOn)

        public FillState ShallowClone() {
            return new FillState() {
                Fill = this.Fill, // shallow clone
                FillPos = this.FillPos,
                BFill = this.BFill,
            };
        }
        public SymbolItem? NextFill() {
            // ala ToneMaker.GetNextLever()

            // note: doesn't give RequireNext; make sure you add that first

            // doesn't treat state as immutable. (make sure to clone first)

            //if (StraightDown != LeverKind.None) {
            //    return null;
            //}

            if (Fill == null || Fill.Length == 0) {
                // nothing queued
                return null;
            }

            var next = Fill[FillPos];
            FillPos = (FillPos + 1) % Fill.Length;

            if (next == LeverKind.Stop) {
                next = LeverKind.None;
                Fill = null;
                FillPos = 0;
            }

            var partialSymbol = new SymbolItem() {
                Lever = next,
                Required = false,
                IambicBMode = BFill,
            };

            return partialSymbol;
        }
    }
}
