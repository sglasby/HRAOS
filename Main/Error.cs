using System;

class Error {
    // A class offering String.Format()-built exceptions
    // TODO: Perhaps better to add overloads to an Exception class???

    public static void Throw(string format, params object[] args) {
        string msg = string.Format(format, args);
        throw new Exception(msg);
    } // Throw()

    public static void BadArg(string format, params object[] args) {
        string msg = string.Format(format, args);
        throw new ArgumentException(msg);
    } // BadArg()

} // class Error
