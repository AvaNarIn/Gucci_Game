public static class ItemSetHelper
{
    public static ItemSet GetSet(ItemData item)
    {
        if (item is DiceData) return ItemSet.Dice;
        if (item is CardData) return ItemSet.Card;
        if (item is ChessData) return ItemSet.Chess;
        if (item is RockPaperScissorsData) return ItemSet.RockPaperScissors;
        if (item is TicTacToeData) return ItemSet.TicTacToe;
        return ItemSet.Dice;
    }
}