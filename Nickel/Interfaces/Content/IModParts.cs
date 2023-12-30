using System;
using System.IO;

namespace Nickel;

public interface IModParts
{
	IPartEntry RegisterPart(string name, Spr onPart, Spr? offPart);
}

