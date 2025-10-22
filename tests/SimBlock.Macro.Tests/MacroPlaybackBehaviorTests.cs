using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SimBlock.Core.Application.Services;
using SimBlock.Core.Domain.Entities;
using SimBlock.Tests.Macros;
using Xunit;

namespace SimBlock.Tests.Macros
{
    public class MacroPlaybackBehaviorTests
    {
        private static (MacroService svc, FakeKeyboardHookService k, FakeMouseHookService m) Create()
        {
            var k = new FakeKeyboardHookService();
            var m = new FakeMouseHookService();
            var svc = new MacroService(NullLogger<MacroService>.Instance, k, m);
            return (svc, k, m);
        }

        [Fact]
        public async Task PlayAsync_Disables_And_Restores_Blocking_When_Previously_Blocked()
        {
            var (svc, k, m) = Create();
            // Simulate both services were blocked prior to playback
            await k.SetBlockingAsync(true, "test");
            await m.SetBlockingAsync(true, "test");

            var macro = new Macro { Name = "P" };
            macro.Events.Add(new MacroEvent { Device = MacroEventDevice.Keyboard, Type = MacroEventType.KeyDown, TimestampMs = 0, VirtualKeyCode = 0x41 });
            macro.Events.Add(new MacroEvent { Device = MacroEventDevice.Keyboard, Type = MacroEventType.KeyUp, TimestampMs = 50, VirtualKeyCode = 0x41 });

            await svc.PlayAsync(macro, CancellationToken.None, speed: 1.0, loops: 1);

            k.SetBlockingCalls.Should().BeGreaterOrEqualTo(2); // disable + restore
            m.SetBlockingCalls.Should().BeGreaterOrEqualTo(2);
            k.LastBlockingValue.Should().BeTrue();
            m.LastBlockingValue.Should().BeTrue();
        }

        [Fact]
        public async Task PlayAsync_Respects_Speed_And_Loops_Timing()
        {
            var (svc, k, m) = Create();
            var macro = new Macro { Name = "Timing" };
            // first event at 0ms, second at 200ms => at 2x speed the delay between is ~100ms per loop
            macro.Events.Add(new MacroEvent { Device = MacroEventDevice.Mouse, Type = MacroEventType.MouseMove, TimestampMs = 0, X = 0, Y = 0 });
            macro.Events.Add(new MacroEvent { Device = MacroEventDevice.Mouse, Type = MacroEventType.MouseMove, TimestampMs = 200, X = 1, Y = 1 });

            var sw = Stopwatch.StartNew();
            await svc.PlayAsync(macro, CancellationToken.None, speed: 2.0, loops: 2);
            sw.Stop();

            // Expected total delay ~100ms per loop => ~200ms total. Allow generous bounds.
            sw.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(150);
            sw.ElapsedMilliseconds.Should().BeLessThan(1000);
        }
    }
}
