using System;

[Serializable]
public struct Currency
{
    public int gold;
    public int silver;
    public int bronze;

    public Currency(int gold, int silver, int bronze)
    {
        this.gold = gold;
        this.silver = silver;
        this.bronze = bronze;
        Normalize();
    }

    public void Normalize()
    {
        silver += bronze / 100;
        bronze %= 100;

        gold += silver / 100;
        silver %= 100;
    }

    public static Currency operator +(Currency a, Currency b)
    {
        return new Currency(a.gold + b.gold, a.silver + b.silver, a.bronze + b.bronze);
    }

    public static Currency operator -(Currency a, Currency b)
    {
        return new Currency(a.gold - b.gold, a.silver - b.silver, a.bronze - b.bronze);
    }

    public static bool operator >=(Currency a, Currency b)
    {
        return a.GetTotalBronze() >= b.GetTotalBronze();
    }

    public static bool operator <=(Currency a, Currency b)
    {
        return a.GetTotalBronze() <= b.GetTotalBronze();
    }

    public int GetTotalBronze()
    {
        return gold * 10000 + silver * 100 + bronze;
    }

    public override string ToString()
    {
        return $"{gold}g {silver}s {bronze}b";
    }
}
