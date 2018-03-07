﻿using System.Diagnostics;
using Yuka.Script.Data;
using Yuka.Util;

namespace Yuka.Script.Syntax.Expr {
	public class StringLiteral : Expression {
		public ScriptValue.Str Value;

		public override string ToString() => '"' + Value.StringValue.Escape() + '"';

		[DebuggerStepThrough]
		public override DataElement Accept(ISyntaxVisitor visitor) => visitor.Visit(this);
	}
}
