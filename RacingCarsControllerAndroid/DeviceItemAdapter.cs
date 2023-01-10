using Android.Views;
using AndroidX.RecyclerView.Widget;
using Plugin.BLE.Abstractions.Contracts;

namespace RacingCarsControllerAndroid
{
    internal class DeviceItemAdapter : RecyclerView.Adapter
    {
        private readonly List<IDevice> _devices;

        public DeviceItemAdapter()
        {
            _devices = new List<IDevice>();
        }

        public DeviceItemAdapter(List<IDevice> devices)
        {
            _devices =  devices;
        }

        public event EventHandler<int> ItemClick;

        public override int ItemCount => _devices.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var device = _devices[position];
            var ivh = holder as ItemViewHolder;
            ivh.LabelTextView.Text = device.Name ?? "";
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var layoutInflater = LayoutInflater.From(parent.Context);
            var itemView = layoutInflater.Inflate(Resource.Layout.device_item, parent, false);
            return new ItemViewHolder(itemView, OnClick);
        }

        public void Replace(List<IDevice> devices)
        {
            _devices.Clear();
            _devices.AddRange(devices);
            NotifyDataSetChanged();
        }

        public void Add(IDevice device)
        {
            _devices.Add(device);
            NotifyItemInserted(_devices.Count - 1);
        }

        public IDevice Get(int position) => _devices[position];

        private void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }
    }

    public class ItemViewHolder : RecyclerView.ViewHolder
    {
        public TextView LabelTextView { get; private set; }

        public ItemViewHolder(View itemView, Action<int> listener) : base(itemView)
        {
            LabelTextView = itemView.FindViewById<TextView>(Resource.Id.itemLabel);
            itemView.Click += (sender, e) => listener(base.LayoutPosition);
        }
    }
}
