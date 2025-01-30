export default function Damage({ damage }) {
  return (
    <div className="absolute bottom-[24px] left-[24px] flex h-8 w-8 items-center justify-center rounded-full border-2 bg-amber-400">
      <span className="text-lg font-bold">-{damage}</span>
    </div>
  );
}
