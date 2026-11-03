using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pockets.Domain;

namespace Pockets.Tests;

[TestClass]
public class PocketStateTests
{
	[TestMethod]
	public void NewState_IsEmpty()
	{
		PocketState state = new PocketState();

		Assert.AreEqual(0, state.OccupiedCount);
		for (int slot = 0; slot < PocketState.SlotCount; slot++)
		{
			Assert.IsFalse(state.IsOccupied(slot));
			Assert.AreEqual(PocketState.NoValuable, state.Get(slot));
		}
	}

	[TestMethod]
	public void TryStore_FreeSlot_Succeeds()
	{
		PocketState state = new PocketState();

		Assert.IsTrue(state.TryStore(1, 42));
		Assert.IsTrue(state.IsOccupied(1));
		Assert.AreEqual(42, state.Get(1));
		Assert.AreEqual(1, state.OccupiedCount);
	}

	[TestMethod]
	public void TryStore_OccupiedSlot_Fails()
	{
		PocketState state = new PocketState();
		state.TryStore(0, 10);

		Assert.IsFalse(state.TryStore(0, 11));
		Assert.AreEqual(10, state.Get(0));
	}

	[TestMethod]
	public void TryStore_SameValuableTwice_Fails()
	{
		PocketState state = new PocketState();
		state.TryStore(0, 10);

		Assert.IsFalse(state.TryStore(1, 10));
		Assert.IsFalse(state.IsOccupied(1));
	}

	[TestMethod]
	public void TryStore_InvalidSlot_Fails()
	{
		PocketState state = new PocketState();

		Assert.IsFalse(state.TryStore(-1, 10));
		Assert.IsFalse(state.TryStore(PocketState.SlotCount, 10));
	}

	[TestMethod]
	public void TryStore_NoValuableKey_Fails()
	{
		PocketState state = new PocketState();

		Assert.IsFalse(state.TryStore(0, PocketState.NoValuable));
	}

	[TestMethod]
	public void TryStore_KeyZero_IsAValidKey()
	{
		// In singleplayer every valuable has view id 0; the integration layer must use another key,
		// but 0 itself and negative keys are legitimate values for the domain.
		PocketState state = new PocketState();

		Assert.IsTrue(state.TryStore(0, 0));
		Assert.IsTrue(state.TryStore(1, -5));
		Assert.AreEqual(0, state.FindSlot(0));
		Assert.AreEqual(1, state.FindSlot(-5));
	}

	[TestMethod]
	public void TryRelease_ReturnsTheStoredKeyAndFreesTheSlot()
	{
		PocketState state = new PocketState();
		state.TryStore(2, 77);

		Assert.IsTrue(state.TryRelease(2, out int key));
		Assert.AreEqual(77, key);
		Assert.IsFalse(state.IsOccupied(2));
	}

	[TestMethod]
	public void TryRelease_EmptyOrInvalidSlot_Fails()
	{
		PocketState state = new PocketState();

		Assert.IsFalse(state.TryRelease(0, out int emptyKey));
		Assert.AreEqual(PocketState.NoValuable, emptyKey);
		Assert.IsFalse(state.TryRelease(9, out _));
	}

	[TestMethod]
	public void FindSlot_UnknownValuable_ReturnsMinusOne()
	{
		PocketState state = new PocketState();
		state.TryStore(0, 10);

		Assert.AreEqual(-1, state.FindSlot(99));
		Assert.AreEqual(-1, state.FindSlot(PocketState.NoValuable));
	}

	[TestMethod]
	public void FirstFreeSlot_SkipsOccupiedSlots()
	{
		PocketState state = new PocketState();
		Assert.AreEqual(0, state.FirstFreeSlot());

		state.TryStore(0, 1);
		state.TryStore(1, 2);
		Assert.AreEqual(2, state.FirstFreeSlot());

		state.TryStore(2, 3);
		Assert.AreEqual(-1, state.FirstFreeSlot());
	}

	[TestMethod]
	public void Clear_EmptiesEverySlot()
	{
		PocketState state = new PocketState();
		state.TryStore(0, 1);
		state.TryStore(1, 2);

		state.Clear();

		Assert.AreEqual(0, state.OccupiedCount);
		Assert.AreEqual(0, state.FirstFreeSlot());
	}

	[TestMethod]
	public void Snapshot_IsACopy()
	{
		PocketState state = new PocketState();
		state.TryStore(0, 5);

		int[] snapshot = state.Snapshot();
		snapshot[0] = 99;

		Assert.AreEqual(5, state.Get(0));
		Assert.AreEqual(PocketState.SlotCount, snapshot.Length);
	}
}
