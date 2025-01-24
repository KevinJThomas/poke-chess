import Hero from "./Hero";

export default function Players() {
  return (
    <div className="absolute top-0 bottom-0 left-2 flex scale-75 flex-col items-center justify-center gap-3">
      <Hero name="Opponent 1" health="30" armor="10" tier="1" />
      <Hero name="Opponent 1" health="30" armor="10" tier="1" />
      <Hero name="Opponent 1" health="30" armor="10" tier="1" />
      <Hero name="Opponent 1" health="30" armor="10" tier="1" />
      <Hero name="Opponent 1" health="30" armor="10" tier="1" />
      <Hero name="Opponent 1" health="30" armor="10" tier="1" />
      <Hero name="Opponent 1" health="30" armor="10" tier="1" />
      <Hero name="Opponent 1" health="30" armor="10" tier="1" />
    </div>
  );
}
