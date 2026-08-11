import Todo from "./todo";

export default function Home() {
  return (
    <div className="relative flex min-h-svh flex-1 items-center justify-center overflow-hidden bg-background px-4 py-8 font-sans sm:px-6 sm:py-12">
      <div
        className="pointer-events-none absolute inset-0 opacity-80 [background-image:radial-gradient(circle_at_20%_10%,rgba(35,134,54,0.14),transparent_32%),radial-gradient(circle_at_85%_85%,rgba(31,111,235,0.08),transparent_28%)]"
        aria-hidden="true"
      />
      <div
        className="pointer-events-none absolute inset-0 opacity-[0.035] [background-image:linear-gradient(rgba(255,255,255,0.6)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,0.6)_1px,transparent_1px)] [background-size:44px_44px]"
        aria-hidden="true"
      />

      <main className="relative w-full max-w-2xl overflow-hidden rounded-3xl border border-white/10 bg-card/90 p-5 shadow-[0_28px_90px_rgba(0,0,0,0.38)] backdrop-blur-xl sm:p-8">
        <div
          className="pointer-events-none absolute inset-x-12 top-0 h-px bg-gradient-to-r from-transparent via-emerald-400/60 to-transparent"
          aria-hidden="true"
        />
        <Todo />
      </main>
    </div>
  );
}
