"use client";

import { Button } from "@/shared/components/ui/button";
import { Input } from "@/shared/components/ui/input";
import useCounter from "@/shared/hooks/use-counter";

export default function Counter() {
  const { counter, click, isWin } = useCounter();
  return (
    <div className="flex flex-col gap-5">
      <div className="flex min-h-56 flex-col items-center justify-center rounded-xl bg-[#181818] text-center ring-1 ring-white/5">
        <p className="text-sm font-medium text-muted-foreground">
          Текущее значение
        </p>
        <CoolCount count={counter} />
        <p className="mt-2 text-xs text-muted-foreground">Цель: 10 нажатий</p>
      </div>

      <Button
        className="h-11 w-full rounded-full bg-primary text-sm font-semibold text-white hover:bg-[#d9002b]"
        onClick={click}
      >
        Увеличить счётчик
      </Button>

      <div>
        <label
          htmlFor="counter-note"
          className="mb-2 block text-sm font-medium text-foreground"
        >
          Заметка
        </label>
        <Input
          id="counter-note"
          type="text"
          placeholder="Добавьте комментарий…"
          className="h-11 w-full rounded-full border-[#303030] bg-[#121212] px-4 focus-visible:border-primary focus-visible:ring-primary/25"
        />
      </div>

      {isWin && (
        <span className="rounded-xl bg-primary/10 px-4 py-3 text-center text-sm font-medium text-[#ff6685] ring-1 ring-primary/20">
          Поздравляем! Вы достигли 10!
        </span>
      )}
    </div>
  );
}

type CoolCountProps = {
  count: number;
};

function CoolCount({ count }: CoolCountProps) {
  return (
    <span className="mt-3 text-7xl font-bold tabular-nums tracking-[-0.06em] text-foreground">
      {count}
    </span>
  );
}
