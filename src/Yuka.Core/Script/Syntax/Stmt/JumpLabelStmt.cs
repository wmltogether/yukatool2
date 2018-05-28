﻿using System.Diagnostics;
using Yuka.Util;

namespace Yuka.Script.Syntax.Stmt {
	public class JumpLabelStmt : Statement {
		public string Name;

		public override string ToString() => $"\n:{Name.EscapeIdentifier()}";

		[DebuggerStepThrough]
		public override void Accept<T>(ISyntaxVisitor<T> visitor) => visitor.Visit(this);
	}
}
