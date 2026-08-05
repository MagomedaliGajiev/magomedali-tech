import { JSX } from "react/jsx-runtime";

function Counter(): JSX.Element {
  const number: number = calculateSum(5, 10);

  return <div>Hello world! The sum of 5 and 10 is: {number}</div>;
}

function calculateSum(a: number, b: number): number {
  return a + b;
}
