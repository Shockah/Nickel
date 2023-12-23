using System;

namespace Nickel;

public interface IModEvents
{
    event EventHandler<Nothing> OnAllModsLoaded;
}
