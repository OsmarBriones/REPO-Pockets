using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pockets.Domain;

namespace Pockets.Tests;

[TestClass]
public class SizePolicyTests
{
	[TestMethod]
	public void Accepts_SameSizeAsLimit()
	{
		Assert.IsTrue(SizePolicy.Accepts(PocketSize.Tiny, PocketSize.Tiny));
		Assert.IsTrue(SizePolicy.Accepts(PocketSize.Big, PocketSize.Big));
	}

	[TestMethod]
	public void Accepts_SmallerThanLimit()
	{
		Assert.IsTrue(SizePolicy.Accepts(PocketSize.Tiny, PocketSize.Medium));
	}

	[TestMethod]
	public void Rejects_LargerThanLimit()
	{
		Assert.IsFalse(SizePolicy.Accepts(PocketSize.Small, PocketSize.Tiny));
		Assert.IsFalse(SizePolicy.Accepts(PocketSize.VeryTall, PocketSize.Tall));
	}

	[TestMethod]
	public void LimitVeryTall_AcceptsEverySize()
	{
		foreach (PocketSize size in System.Enum.GetValues(typeof(PocketSize)))
		{
			Assert.IsTrue(SizePolicy.Accepts(size, PocketSize.VeryTall), size.ToString());
		}
	}

	[TestMethod]
	public void LimitTiny_AcceptsOnlyTiny()
	{
		int accepted = 0;
		foreach (PocketSize size in System.Enum.GetValues(typeof(PocketSize)))
		{
			if (SizePolicy.Accepts(size, PocketSize.Tiny))
			{
				accepted++;
			}
		}

		Assert.AreEqual(1, accepted);
	}

	[TestMethod]
	public void TryFromGameVolume_MapsTheGameOrder()
	{
		Assert.IsTrue(SizePolicy.TryFromGameVolume(0, out PocketSize tiny));
		Assert.AreEqual(PocketSize.Tiny, tiny);
		Assert.IsTrue(SizePolicy.TryFromGameVolume(6, out PocketSize veryTall));
		Assert.AreEqual(PocketSize.VeryTall, veryTall);
	}

	[TestMethod]
	public void TryFromGameVolume_RejectsOutOfRange()
	{
		Assert.IsFalse(SizePolicy.TryFromGameVolume(-1, out _));
		Assert.IsFalse(SizePolicy.TryFromGameVolume(7, out _));
	}
}
