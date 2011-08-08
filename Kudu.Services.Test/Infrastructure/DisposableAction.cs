using System;

namespace Kudu.Services.Test.Infrastructure {
    public class DisposableAction : IDisposable {
        private readonly Action _action;
        public DisposableAction(Action action) {
            _action = action;
        }

        public void Dispose() {
            _action();
        }
    }
}
