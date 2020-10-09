using System.Linq;
using System.Text;
using Mono.Cecil;

namespace SimpleDeobfuscator
{
    public static class LiteralRenamer
    {
        public static void FixNaming(ModuleDefinition module)
        {
            foreach (var type in module.GetTypes())
                FixNaming(type);
        }

        private static void FixNaming(TypeDefinition type)
        {
            var currentTypeName = type.Name;
            var newTypeName = TryFixName(currentTypeName);
            if (newTypeName != currentTypeName)
            {
                type.Name = newTypeName;
            }

            foreach (var nestedType in type.NestedTypes)
                FixNaming(nestedType);

            foreach (var typeParameter in type.GenericParameters)
                FixNaming(typeParameter);

            foreach (var field in type.Fields)
                FixNaming(field);
            foreach (var method in type.Properties)
                FixNaming(method);
            foreach (var method in type.Methods)
            {
                FixNaming(method);
                foreach (var methodParameter in method.Parameters)
                    FixNaming(methodParameter);
                foreach (var methodGenericParameter in method.GenericParameters)
                    FixNaming(methodGenericParameter);
            }
        }

        private static void FixNaming(ParameterReference member)
        {
            var currentName = member.Name;
            var newName = TryFixName(currentName);
            if (newName != currentName)
            {
                member.Name = newName;
            }
        }

        private static void FixNaming(MemberReference member)
        {
            var currentName = member.Name;
            var newName = TryFixName(currentName);
            if (newName != currentName)
            {
                member.Name = newName;
            }
        }

        /// <summary>
        /// Fix non-printed symbols in <paramref name="name"/> (if they exists).
        /// </summary>
        private static string TryFixName(string name)
        {
            if (name.Length == 0)
                return "";

            var newName = new StringBuilder(name.Length);

            if (name[0] == '_' || char.IsLetter(name[0]))
                newName.Append(name[0]);
            else
                newName.AppendCharAsPrintable(name[0]);

            foreach (var с in name.Skip(1))
                if (с == '_' || с == '.' || char.IsLetterOrDigit(с))
                    newName.Append(с);
                else
                    newName.AppendCharAsPrintable(с);

            return newName.ToString();
        }

        /// <summary>
        /// Convert char <paramref name="c"/> to printable set of chars, that can be part of member name.
        /// </summary>
        private static void AppendCharAsPrintable(this StringBuilder stringBuilder, char c)
        {
            stringBuilder.Append(GetCharForCode(c & 31)); // bits 0..4
            c >>= 5;
            stringBuilder.Append(GetCharForCode(c & 63)); // bits 5..10
            c >>= 6;
            stringBuilder.Append(GetCharForCode(c & 63)); // bits 11..15
        }

        /// <summary>
        /// Convert 6-bit value to text in XXEncode-like style.
        /// </summary>
        /// <param name="code">6-bit value in interval [0..63] that will be converted to symbol.</param>
        /// <returns>Symbol, that definitely can be a part of method name.</returns>
        /// <remarks>for 5-bit value [0..31] returns only letters.</remarks>
        private static char GetCharForCode(int code)
        {
            const int latinLettersCount = 'Z' - 'A' + 1; // 27

            if (code < latinLettersCount) // 0 .. 26
                return (char)('A' + code);
            code -= latinLettersCount;
            if (code < latinLettersCount) // 27 .. 53
                return (char)('a' + code);
            code -= latinLettersCount;
            return (char)('0' + code); // 54 .. 63
        }
    }
}
