using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MoLibrary.GachaPool.Conventions;
public abstract class Card
{
    /// <summary>
    /// The card rarity. The global probability will auto generate base on this.
    /// </summary>
    public CardRarity Rarity { get; set; }

    /// <summary>
    /// The probability is relative to all the cards which have the same rarity with the card. Only available when
    /// the SetProbability property is null.
    /// </summary>
    public double? RatioAmountSameRarity
    {
        get;
        set
        {
            if (value != null && PresetProbability != null)
            {
                throw new InvalidOperationException("Cannot set RatioAmountSameRarity when SetProbability is not null.");
            }
            field = value;
        }
    }

    /// <summary>
    /// The card real probability relative to the entire card pool.
    /// </summary>
    public double RealProbability { get; internal set; }

    /// <summary>
    /// If not set, the real probability will auto generate according to rarity and the whole cards pool. The probability
    /// is relative to the entire card pool and not to the corresponding rarity cards.
    /// </summary>
    public double? PresetProbability { get; set; }

    /// <summary>
    /// Gets a value indicating whether this card is a "NothingCard".
    /// A "NothingCard" represents the absence of a valid card in a card pool.
    /// </summary>
    public bool IsNotingCard => this is NothingCard;
    /// <summary>
    /// Indicate that the card has been removed and will not appear at card pool.
    /// (or to say the probability has becomes zero)
    /// </summary>
    public bool IsRemoved
    {
        get => (Attributes & CardAttributes.Removed) != 0;
        internal set
        {
            if (value == false)
            {
                Attributes &= ~CardAttributes.Removed;
            }
            else
            {
                Attributes |= CardAttributes.Removed;
                RealProbability = 0;
            }
        }
    }

    public CardAttributes Attributes { get; private set; }

    #region LimitedCard

    private int _remainCount;

    /// <summary>
    /// If set total count, it means the card is limited, and its probability will become zero when this
    /// kind of cards run out. Default is null which means is infinite.
    /// </summary>
    public int TotalCount
    {
        get;
        set
        {
            field = value;
            _remainCount = value;
            IsLimitedCard = value > 0;
        }
    }

    /// <summary>
    /// The limited card remaining count.
    /// </summary>
    public int RemainCount => _remainCount;
    /// <summary>
    /// This property will auto turn to true when you set the card TotalCount property.
    /// </summary>
    public bool IsLimitedCard
    {
        get => (Attributes & CardAttributes.Limited) != 0;
        private set
        {
            if (value == false)
            {
                Attributes &= ~CardAttributes.Limited;
            }
            else
            {
                Attributes |= CardAttributes.Limited;
            }
        }
    }
    /// <summary>
    /// Try to subtract the number of cards to ensure that the card was successfully obtained.
    /// Only available when the card is limited card.
    /// </summary>
    /// <returns></returns>
    internal bool SuccessGetCard()
    {
        if (!IsLimitedCard) return true;
        if (_remainCount <= 0) return false;
        return Interlocked.Decrement(ref _remainCount) >= 0;
    }

    #endregion




    public override string ToString()
    {
        return $"{RealProbability:P5} [{Rarity}]";
    }


    public abstract string GetCardName();
}

/// <summary>
/// When a cards pool placed all valid card, the rest probability will turn to a nothing card.
/// </summary>
public class NothingCard : Card
{
    public NothingCard()
    {

    }
    public NothingCard(double remainRealProbability)
    {
        RealProbability = remainRealProbability;
    }

    public override bool Equals(object? obj)
    {
        return obj is NothingCard;
    }

    public override int GetHashCode()
    {
        return 13131313;//Always equal when the card is nothing card.
    }
    public override string ToString()
    {
        return $"{GetCardName()} ———— {base.ToString()}";
    }

    public override string GetCardName()
    {
        return "Nothing";
    }
}
/// <summary>
/// Represents a generic card with a strongly-typed value.
/// </summary>
/// <typeparam name="T">The type of the card's value.</typeparam>
public class Card<T> : Card where T : notnull
{
    /// <summary>
    /// Gets or sets the value associated with this card.
    /// </summary>
    public T CardInfo { get; }

    /// <summary>
    /// Initializes a new instance of the Card{T} class.
    /// </summary>
    /// <param name="cardInfo">The value to associate with this card.</param>
    public Card(T cardInfo)
    {
        CardInfo = cardInfo;
    }

    /// <summary>
    /// Initializes a new instance of the Card{T} class.
    /// </summary>
    /// <param name="rarity"></param>
    /// <param name="cardInfo"></param>
    public Card(CardRarity rarity, T cardInfo)
    {
        CardInfo = cardInfo;
        Rarity = rarity;
    }

    /// <summary>
    /// Creates multiple cards of the same rarity with different values.
    /// </summary>
    /// <param name="rarity">The rarity to assign to all created cards.</param>
    /// <param name="cards">The values to create cards from.</param>
    /// <returns>A list of created cards.</returns>
    public static List<Card<T>> CreateMultiCards(CardRarity rarity, params T[] cards)
    {
        return cards.Select(card => new Card<T>(rarity, card)).ToList();
    }

    public override bool Equals(object? obj)
    {
        if (obj is Card<T> card)
        {
            return EqualityComparer<T>.Default.Equals(card.CardInfo, CardInfo);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return EqualityComparer<T>.Default.GetHashCode(CardInfo);
    }

    public override string ToString()
    {
        return $"{GetCardName()} ———— {base.ToString()}";
    }

    public override string GetCardName()
    {
        return CardInfo.ToString() ?? "UNF";
    }

    public static Card CreateCard(CardRarity rarity, T card)
    {
        return new Card<T>(card) { Rarity = rarity };
    }
}

/// <summary>
/// Extension methods for card collections.
/// </summary>
public static class CardExtension
{
    /// <summary>
    /// Performs an action on each card in a collection.
    /// </summary>
    /// <param name="cards">The collection of cards to process.</param>
    /// <param name="action">The action to perform on each card.</param>
    /// <returns>The original collection of cards.</returns>
    public static ICollection<Card> EachCardSet(this ICollection<Card> cards, Action<Card> action)
    {
        foreach (var card in cards)
        {
            action(card);
        }
        return cards;
    }
    /// <summary>
    /// Performs an action on each strongly-typed card in a collection.
    /// </summary>
    /// <typeparam name="T">The type of the cards' values.</typeparam>
    /// <param name="cards">The collection of cards to process.</param>
    /// <param name="action">The action to perform on each card.</param>
    /// <returns>The original collection of cards.</returns>
    public static ICollection<Card<T>> EachCardSet<T>(this ICollection<Card<T>> cards, Action<Card<T>> action) where T : notnull
    {
        foreach (var card in cards)
        {
            action(card);
        }
        return cards;
    }
}