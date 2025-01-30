export default function HeroDamage({ damage }) {
  return (
    <div className="absolute bottom-[24px] left-[24px] flex h-12 w-12 items-center justify-center rounded-full border-2 bg-amber-400">
      <span className="text-xl font-bold">-{damage}</span>
    </div>
  );
}
