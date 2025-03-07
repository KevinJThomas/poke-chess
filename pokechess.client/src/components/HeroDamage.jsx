export default function HeroDamage({ damage }) {
  return (
    <div
      style={{ minWidth: 48 }}
      className="animate-jump-in absolute bottom-[24px] left-[24px] flex h-12 items-center justify-center rounded-lg border-2 bg-amber-400"
    >
      <span className="text-xl font-bold">-{damage}</span>
    </div>
  );
}
