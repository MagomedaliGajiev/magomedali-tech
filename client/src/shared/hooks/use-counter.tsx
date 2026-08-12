import { useState } from "react";

export default function useCounter() {
  const [counter, setCounter] = useState(0);

  const click = () => {
    setCounter(counter + 1);
  };
  const isWin = counter >= 10;

  return { counter, click, isWin };
}
