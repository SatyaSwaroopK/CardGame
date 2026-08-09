using System.Collections.Generic;
using UnityEngine;

public class AbilityResult
{
    public int power;
    public int bonusPoints;
    public int stealPoints;
}


public interface ICardAbility
{
    AbilityResult Resolve(
        CardData card
    );
}


// =========================================
// NO ABILITY
// =========================================

public class NoAbility : ICardAbility
{
    public AbilityResult Resolve(
        CardData card)
    {
        return new AbilityResult
        {
            power = card.power
        };
    }
}


// =========================================
// GAIN POINTS
// =========================================

public class GainPointsAbility : ICardAbility
{
    public AbilityResult Resolve(
        CardData card)
    {
        return new AbilityResult
        {
            power = card.power,

            bonusPoints =
                card.ability.value
        };
    }
}


// =========================================
// STEAL POINTS
// =========================================

public class StealPointsAbility : ICardAbility
{
    public AbilityResult Resolve(
        CardData card)
    {
        return new AbilityResult
        {
            power = card.power,

            stealPoints =
                card.ability.value
        };
    }
}


// =========================================
// DOUBLE POWER
// =========================================

public class DoublePowerAbility : ICardAbility
{
    public AbilityResult Resolve(
        CardData card)
    {
        int multiplier =
            Mathf.Max(
                2,
                card.ability.value
            );

        return new AbilityResult
        {
            power =
                card.power *
                multiplier
        };
    }
}


// =========================================
// RESOLVER
// =========================================

public static class AbilityResolver
{
    private static readonly
        Dictionary<string, ICardAbility>
        abilities =
            new Dictionary<string, ICardAbility>
            {
                {
                    "GainPoints",
                    new GainPointsAbility()
                },

                {
                    "StealPoints",
                    new StealPointsAbility()
                },

                {
                    "DoublePower",
                    new DoublePowerAbility()
                }
            };


    public static AbilityResult Resolve(
        CardData card)
    {
        if (card == null)
        {
            return new AbilityResult();
        }


        if (card.ability == null ||
            string.IsNullOrEmpty(
                card.ability.type))
        {
            return new NoAbility()
                .Resolve(card);
        }


        if (abilities.TryGetValue(
                card.ability.type,
                out ICardAbility ability))
        {
            return ability.Resolve(card);
        }


        Debug.LogWarning(
            "Unknown ability: " +
            card.ability.type
        );


        return new NoAbility()
            .Resolve(card);
    }
}