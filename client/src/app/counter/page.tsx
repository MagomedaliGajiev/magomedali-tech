import Counter from "../../components/counter";
import { Sigma } from "lucide-react";

export default function CounterPage() {
  return (
    <section className="mx-auto max-w-2xl">
      <div className="mb-5 flex items-center gap-4">
        <span className="flex size-12 items-center justify-center rounded-full bg-primary text-white">
          <Sigma className="size-6" aria-hidden="true" />
        </span>
        <div>
          <h1 className="text-2xl font-bold tracking-[-0.03em]">Счётчик</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Наберите 10 очков, чтобы выполнить цель
          </p>
        </div>
      </div>

      <div className="w-full rounded-xl bg-card p-5 ring-1 ring-white/5 sm:p-8">
        <Counter />
      </div>
    </section>
  );
}
