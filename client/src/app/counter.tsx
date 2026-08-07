"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Field } from "@base-ui/react";
import { Input } from "@/components/ui/input";

export default function Counter() {
  const [count, setCount] = useState(0);

  const handleClick = () => {
    setCount((currentCount) => currentCount + 1);
  };

  return (
    <div className="flex flex-col items-center gap-5 text-center">
      <p className="text-sm font-medium text-muted-foreground">
        Текущее значение
      </p>
      <CoolCount count={count} />
      <Button
        className="w-full border border-[#3fb950]/30"
        onClick={handleClick}
      >
        Увеличить
      </Button>

      <Input
        type="text"
        placeholder="Введите текст"
        className="w-full border border-[#3fb950]/30"
      />

      {count >= 10 && <span>Поздравляем! Вы достигли 10!</span>}
    </div>
  );
}

type CoolCountProps = {
  count: number;
};

function CoolCount({ count }: CoolCountProps) {
  return (
    <span className="text-4xl font-semibold tabular-nums text-foreground">
      {count}
    </span>
  );
}
