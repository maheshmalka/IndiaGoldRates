interface RateCardProps {
  label: string;
  priceInrPerGram: number | undefined;
}

const inrFormatter = new Intl.NumberFormat("en-IN", {
  style: "currency",
  currency: "INR",
  maximumFractionDigits: 2,
});

export function RateCard({ label, priceInrPerGram }: RateCardProps) {
  return (
    <div className="rate-card">
      <div className="rate-card-label">{label}</div>
      <div className="rate-card-price">
        {priceInrPerGram !== undefined ? inrFormatter.format(priceInrPerGram) : "—"}
      </div>
      <div className="rate-card-unit">per gram</div>
    </div>
  );
}
