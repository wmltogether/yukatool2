﻿using System.Diagnostics;

namespace Yuka.Script.Syntax.Stmt {
	public class BodyFunctionStmt : Statement {
		public FunctionCallStmt Function;
		public BlockStmt Body;

		public override string ToString() => $"{Function.ToString().TrimEnd(';')} {Body}";

		[DebuggerStepThrough]
		public override void Accept(ISyntaxVisitor visitor) => visitor.Visit(this);
	}
}
