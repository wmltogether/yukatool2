﻿using System.Collections.Generic;

namespace Yuka.Script.Instructions {
	public class InstructionList : List<Instruction> {
		public uint MaxLocals;

		public InstructionList(uint maxLocals) {
			MaxLocals = maxLocals;
		}
	}
}
