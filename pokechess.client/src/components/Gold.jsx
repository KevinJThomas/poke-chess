export default function Gold({ gold, maxGold }) {
  return (
    <div className="absolute right-0 bottom-0 flex h-6 w-16 items-center justify-center rounded-xl bg-amber-400">
      <span>
        {gold} / {maxGold}
      </span>
    </div>
  );
}
