using System;

namespace Nickel;

public interface ICardEntry : IModOwned
{
    CardConfiguration Configuration { get; }
}
