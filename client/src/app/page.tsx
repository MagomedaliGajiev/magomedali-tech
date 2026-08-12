import { Play } from "lucide-react";

export default function Home() {
  return (
    <section>
      <div className="flex flex-col gap-3 border-b border-white/10 pb-7">
        <div className="flex items-center gap-2 text-sm font-medium text-primary">
          <Play className="size-4 fill-current" aria-hidden="true" />
          Ваш личный марафон
        </div>
        <h1 className="max-w-2xl text-3xl font-bold tracking-[-0.04em] sm:text-4xl">
          Продолжайте двигаться вперёд
        </h1>
        <p className="max-w-xl text-sm leading-6 text-muted-foreground sm:text-base">
          Личное пространство для ваших идей и будущих проектов в привычном
          тёмном интерфейсе.
        </p>
      </div>
    </section>
  );
}
